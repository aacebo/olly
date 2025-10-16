using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Octokit.Internal;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Errors;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public class GithubTokenRefreshHandler(IServiceProvider provider, Account account) : DelegatingHandler(HttpMessageHandlerFactory.CreateDefault())
{
    private Octokit.GitHubClient AppClient { get; init; } = provider.GetRequiredService<Octokit.GitHubClient>();
    private IAccountService Accounts { get; init; } = provider.GetRequiredService<IAccountService>();
    private ILogger<GithubTokenRefreshHandler> Logger { get; init; } = provider.GetRequiredService<ILogger<GithubTokenRefreshHandler>>();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Octokit.AuthorizationException ex)
        {
            if (account.Data is GithubAccountData data)
            {
                var accessToken = await AppClient.GitHubApps.CreateInstallationToken(data.Install.Id);
                data.AccessToken = accessToken;
                account.Data = data;
                account = await Accounts.Update(account, cancellationToken);
                request.Headers.Authorization = new("Bearer", accessToken.Token);
                return await SendAsync(request, cancellationToken);
            }

            Logger.LogWarning("{}", ex);
            throw HttpException.UnAuthorized(ex).AddMessage(ex.Message);
        }
    }
}