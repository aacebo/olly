using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Drivers.Models;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public partial class GithubDriver(IServiceProvider provider) : IChatDriver
{
    public SourceType Type => SourceType.Github;

    private GithubService Github => provider.GetRequiredService<GithubService>();
    private IRecordService Records => provider.GetRequiredService<IRecordService>();

    public Task UnInstall(UnInstallRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SignIn(SignInRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}