using Json.More;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Octokit;

using OS.Agent.Models;
using OS.Agent.Settings;
using OS.Agent.Stores;

namespace OS.Agent.Controllers;

[Route("/api/github")]
[ApiController]
public class GithubController(GitHubClient github, IOptions<GithubSettings> settings, IStorage storage) : ControllerBase
{
    [HttpGet("redirect")]
    public async Task<IResult> OnRedirect([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var tokenState = Token.State.Decode(state);
        var tenant = await storage.Tenants.GetById(tokenState.TenantId, cancellationToken);

        if (tenant is null)
        {
            throw new UnauthorizedAccessException();
        }

        var account = tokenState.AccountId is not null
            ? await storage.Accounts.GetById(tokenState.AccountId.Value, cancellationToken)
            : null;

        var res = await github.Oauth.CreateAccessToken(new(settings.Value.ClientId, settings.Value.ClientSecret, code)
        {
            RedirectUri = new Uri(settings.Value.RedirectUrl)
        }, cancellationToken);

        var client = new GitHubClient(new ProductHeaderValue("TOS-Agent"))
        {
            Credentials = new Credentials(
                res.AccessToken,
                AuthenticationType.Bearer
            )
        };

        var user = await client.User.Current();

        if (account is null)
        {
            account = await storage.Accounts.Create(new()
            {
                UserId = tokenState.UserId,
                TenantId = tokenState.TenantId,
                SourceType = SourceType.Github,
                SourceId = user.NodeId,
                Name = user.Login,
                Data = user.ToJsonDocument()
            }, cancellationToken: cancellationToken);
        }

        var token = await storage.Tokens.GetByAccountId(account.Id, cancellationToken);

        if (token is null)
        {
            await storage.Tokens.Create(new()
            {
                AccountId = account.Id,
                Type = res.TokenType,
                AccessToken = res.AccessToken,
                RefreshToken = res.RefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(res.ExpiresIn),
                RefreshExpiresAt = DateTimeOffset.UtcNow.AddSeconds(res.RefreshTokenExpiresIn)
            }, cancellationToken: cancellationToken);
        }
        else
        {
            token.Type = res.TokenType;
            token.AccessToken = res.AccessToken;
            token.RefreshToken = res.RefreshToken;
            token.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(res.ExpiresIn);
            token.RefreshExpiresAt = DateTimeOffset.UtcNow.AddSeconds(res.RefreshTokenExpiresIn);
            await storage.Tokens.Update(token, cancellationToken: cancellationToken);
        }

        return Results.Ok();
    }
}