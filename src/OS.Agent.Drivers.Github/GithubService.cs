using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public class GithubService(Octokit.GitHubClient appClient, IInstallService installService)
{
    public async Task<Octokit.Connection> GetRestConnection(Install install, CancellationToken cancellationToken = default)
    {
        if (install.ExpiresAt is null || install.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            var accessToken = await appClient.GitHubApps.CreateInstallationToken(long.Parse(install.SourceId));
            install.AccessToken = accessToken.Token;
            install.ExpiresAt = accessToken.ExpiresAt;
            install = await installService.Update(install, cancellationToken);
        }

        return new Octokit.Connection(new("TOS-Agent"))
        {
            Credentials = new Octokit.Credentials(
                install.AccessToken,
                Octokit.AuthenticationType.Bearer
            )
        };
    }

    public async Task<Octokit.GraphQL.Connection> GetGraphConnection(Install install, CancellationToken cancellationToken = default)
    {
        if (install.ExpiresAt is null || install.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            var accessToken = await appClient.GitHubApps.CreateInstallationToken(long.Parse(install.SourceId));
            install.AccessToken = accessToken.Token;
            install.ExpiresAt = accessToken.ExpiresAt;
            install = await installService.Update(install, cancellationToken);
        }

        return new Octokit.GraphQL.Connection(new("TOS-Agent"), install.AccessToken);
    }
}