using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Drivers.Models;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public partial class GithubDriver(IServiceProvider provider) : IChatDriver
{
    public SourceType Type => SourceType.Github;

    private Octokit.GitHubClient AppClient => provider.GetRequiredService<Octokit.GitHubClient>();
    private IInstallService Installs => provider.GetRequiredService<IInstallService>();

    public Task Install(InstallRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task UnInstall(UnInstallRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SignIn(SignInRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}