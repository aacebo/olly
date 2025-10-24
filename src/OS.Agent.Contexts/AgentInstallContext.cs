using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Drivers;
using OS.Agent.Storage.Models;

namespace OS.Agent.Contexts;

/// <summary>
/// The context created to handle agent installation events
/// </summary>
public class AgentInstallContext : AgentContext<IDriver>
{
    public required Tenant Tenant { get; init; }
    public required Account Account { get; init; }
    public required User User { get; init; }
    public required Install Installation { get; init; }

    public AgentInstallContext(SourceType type, IServiceScopeFactory factory) : base(type, factory)
    {

    }

    public AgentInstallContext(SourceType type, IServiceScope scope, CancellationToken cancellationToken = default) : base(type, scope, cancellationToken)
    {

    }

    public async Task Install()
    {
        await Driver.Install(new()
        {
            Tenant = Tenant,
            Account = Account,
            Install = Installation
        }, CancellationToken);
    }

    public async Task UnInstall()
    {
        await Driver.UnInstall(new()
        {
            Tenant = Tenant,
            Account = Account,
            Install = Installation
        }, CancellationToken);
    }
}