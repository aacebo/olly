using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Octokit;

using OS.Agent.Models;
using OS.Agent.Services;
using OS.Agent.Settings;

namespace OS.Agent.Controllers;

[Route("/api/github")]
[ApiController]
public class GithubController(IHttpContextAccessor accessor) : ControllerBase
{
    private GitHubClient AppClient => accessor.HttpContext!.RequestServices.GetRequiredService<GitHubClient>();
    private GithubSettings Settings => accessor.HttpContext!.RequestServices.GetRequiredService<IOptions<GithubSettings>>().Value;
    private ITenantService Tenants => accessor.HttpContext!.RequestServices.GetRequiredService<ITenantService>();
    private IAccountService Accounts => accessor.HttpContext!.RequestServices.GetRequiredService<IAccountService>();
    private ITokenService Tokens => accessor.HttpContext!.RequestServices.GetRequiredService<ITokenService>();

    [HttpGet("redirect")]
    public async Task<IResult> OnRedirect([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var tokenState = Token.State.Decode(state);
        var tenant = await Tenants.GetById(tokenState.TenantId, cancellationToken) ?? throw new UnauthorizedAccessException("tenant not found");
        var account = tokenState.AccountId is not null
            ? await Accounts.GetById(tokenState.AccountId.Value, cancellationToken)
            : null;

        var res = await AppClient.Oauth.CreateAccessToken(new(Settings.ClientId, Settings.ClientSecret, code)
        {
            RedirectUri = new Uri(Settings.RedirectUrl)
        }, cancellationToken);

        if (res.Error is not null)
        {
            throw new UnauthorizedAccessException(res.Error);
        }

        var client = new GitHubClient(new ProductHeaderValue("TOS-Agent"))
        {
            Credentials = new Credentials(
                res.AccessToken,
                AuthenticationType.Bearer
            )
        };

        var user = await client.User.Current();
        var install = await client.GitHubApps.GetUserInstallationForCurrent(user.Login);

        if (account is null)
        {
            account = await Accounts.Create(new()
            {
                UserId = tokenState.UserId,
                TenantId = tenant.Id,
                SourceType = SourceType.Github,
                SourceId = user.NodeId,
                Name = user.Login,
                Data = new Data.Account()
            }, cancellationToken);
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

        if (!tenant.Sources.Any(s => s.Type == SourceType.Github && s.Id == install.Id.ToString()))
        {
            tenant.Sources.Add(new()
            {
                Id = install.Id.ToString(),
                Type = SourceType.Github
            });

            await Tenants.Update(tenant, cancellationToken);
        }

        return Results.Ok();
    }
}