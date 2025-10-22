using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Octokit.Internal;

using OS.Agent.Errors;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public class GithubTokenRefreshHandler(IServiceProvider provider, Install? install = null) : DelegatingHandler(HttpMessageHandlerFactory.CreateDefault())
{
    private Octokit.GitHubClient AppClient { get; init; } = provider.GetRequiredService<Octokit.GitHubClient>();
    private IInstallService Installs { get; init; } = provider.GetRequiredService<IInstallService>();
    private ILogger<GithubTokenRefreshHandler> Logger { get; init; } = provider.GetRequiredService<ILogger<GithubTokenRefreshHandler>>();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Octokit.AuthorizationException ex)
        {
            if (install is not null)
            {
                var accessToken = await AppClient.GitHubApps.CreateInstallationToken(long.Parse(install.SourceId));
                install.AccessToken = accessToken.Token;
                install.ExpiresAt = accessToken.ExpiresAt;
                install = await Installs.Update(install, cancellationToken);
                request.Headers.Authorization = new("Bearer", accessToken.Token);
                return await SendAsync(request, cancellationToken);
            }

            Logger.LogWarning("{}", ex);
            throw HttpException.UnAuthorized(ex).AddMessage(ex.Message);
        }
    }
}