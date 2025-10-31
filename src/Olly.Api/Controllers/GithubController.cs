using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Octokit;

using Olly.Drivers.Github.Models;
using Olly.Drivers.Github.Settings;
using Olly.Errors;
using Olly.Services;
using Olly.Storage.Models;

namespace Olly.Api.Controllers;

[Route("/api/github")]
[ApiController]
public class GithubController(IHttpContextAccessor accessor) : ControllerBase
{
    private GitHubClient AppClient => accessor.HttpContext!.RequestServices.GetRequiredService<GitHubClient>();
    private GithubSettings Settings => accessor.HttpContext!.RequestServices.GetRequiredService<IOptions<GithubSettings>>().Value;
    private ITenantService Tenants => accessor.HttpContext!.RequestServices.GetRequiredService<ITenantService>();
    private IAccountService Accounts => accessor.HttpContext!.RequestServices.GetRequiredService<IAccountService>();
    private IChatService Chats => accessor.HttpContext!.RequestServices.GetRequiredService<IChatService>();
    private IMessageService Messages => accessor.HttpContext!.RequestServices.GetRequiredService<IMessageService>();
    private IInstallService Installs => accessor.HttpContext!.RequestServices.GetRequiredService<IInstallService>();
    private ITokenService Tokens => accessor.HttpContext!.RequestServices.GetRequiredService<ITokenService>();
    private IUserService Users => accessor.HttpContext!.RequestServices.GetRequiredService<IUserService>();

    [HttpGet("redirect")]
    public async Task<IResult> OnRedirect([FromQuery] string code, [FromQuery] string state, [FromQuery(Name = "installation_id")] long installationId, CancellationToken cancellationToken)
    {
        var tokenState = Token.State.Decode(state);
        var tenant = await Tenants.GetById(tokenState.TenantId, cancellationToken) ?? throw HttpException.UnAuthorized().AddMessage("tenant not found");
        var user = await Users.GetById(tokenState.UserId, cancellationToken) ?? throw HttpException.UnAuthorized().AddMessage("user not found");
        var message = await Messages.GetById(tokenState.MessageId, cancellationToken) ?? throw HttpException.UnAuthorized().AddMessage("message not found");
        var res = await AppClient.Oauth.CreateAccessToken(new(Settings.ClientId, Settings.ClientSecret, code)
        {
            RedirectUri = new Uri(Settings.RedirectUrl)
        }, cancellationToken);

        if (res.Error is not null)
        {
            throw HttpException.UnAuthorized().AddMessage(res.Error);
        }

        var client = new GitHubClient(new ProductHeaderValue("TOS-Agent"))
        {
            Credentials = new Credentials(
                res.AccessToken,
                AuthenticationType.Bearer
            )
        };

        var githubUser = await client.User.Current();
        var account = await Accounts.GetBySourceId(tenant.Id, SourceType.Github, githubUser.NodeId, cancellationToken);
        var chat = await Chats.GetById(message.ChatId, cancellationToken) ?? throw HttpException.NotFound();
        var githubInstall = await AppClient.GitHubApps.GetInstallationForCurrent(installationId);
        var githubAccessToken = await AppClient.GitHubApps.CreateInstallationToken(installationId);

        if (user.Name is null)
        {
            user.Name = githubUser.Login;
            user = await Users.Update(user, cancellationToken);
        }

        if (account is null)
        {
            account = await Accounts.Create(new()
            {
                TenantId = tenant.Id,
                SourceType = SourceType.Github,
                SourceId = githubUser.NodeId,
                Url = githubUser.Url,
                Name = githubUser.Login,
                Entities = [
                    new GithubUserEntity()
                    {
                        User = new()
                        {
                            Id = githubInstall.Account.Id,
                            NodeId = githubInstall.Account.NodeId,
                            Type = githubInstall.Account.Type?.ToString(),
                            Login = githubInstall.Account.Login,
                            Name = githubInstall.Account.Name,
                            Email = githubInstall.Account.Email,
                            Url = githubInstall.Account.Url,
                            AvatarUrl = githubInstall.Account.AvatarUrl
                        }
                    }
                ]
            }, cancellationToken);
        }

        var install = await Installs.GetBySourceId(SourceType.Github, installationId.ToString(), cancellationToken);

        if (install is null)
        {
            install = await Installs.Create(new()
            {
                UserId = user.Id,
                AccountId = account.Id,
                MessageId = message.Id,
                SourceType = SourceType.Github,
                SourceId = installationId.ToString(),
                Url = githubInstall.HtmlUrl,
                AccessToken = githubAccessToken.Token,
                ExpiresAt = githubAccessToken.ExpiresAt,
                Entities = [
                    new GithubInstallEntity()
                    {
                        Install = githubInstall,
                        AccessToken = githubAccessToken
                    }
                ]
            }, cancellationToken);
        }
        else
        {
            install.AccessToken = githubAccessToken.Token;
            install.ExpiresAt = githubAccessToken.ExpiresAt;
            install.Url = githubInstall.HtmlUrl;
            install.Entities.Put(new GithubInstallEntity()
            {
                Install = githubInstall,
                AccessToken = githubAccessToken
            });

            install = await Installs.Update(install, cancellationToken);
        }

        var token = await Tokens.GetByAccountId(account.Id, cancellationToken);

        if (token is null)
        {
            await Tokens.Create(new()
            {
                AccountId = account.Id,
                Type = res.TokenType,
                AccessToken = res.AccessToken,
                RefreshToken = res.RefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(res.ExpiresIn),
                RefreshExpiresAt = DateTimeOffset.UtcNow.AddSeconds(res.RefreshTokenExpiresIn)
            }, cancellationToken);
        }
        else
        {
            token.Type = res.TokenType;
            token.AccessToken = res.AccessToken;
            token.RefreshToken = res.RefreshToken;
            token.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(res.ExpiresIn);
            token.RefreshExpiresAt = DateTimeOffset.UtcNow.AddSeconds(res.RefreshTokenExpiresIn);
            await Tokens.Update(token, cancellationToken);
        }

        if (!tenant.Sources.Any(s => s.Type == SourceType.Github && s.Id == installationId.ToString()))
        {
            tenant.Sources.Add(new()
            {
                Id = installationId.ToString(),
                Type = SourceType.Github,
                Url = githubInstall.HtmlUrl
            });

            await Tenants.Update(tenant, cancellationToken);
        }

        return Results.Ok();
    }
}