using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Drivers.Github.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public partial class GithubClient(IServiceProvider provider, CancellationToken cancellationToken = default) : Client(SourceType.Github, provider, cancellationToken)
{
    public required GithubEvent Event { get; init; }
    public override Tenant Tenant => Event.Tenant;
    public override Account Account => Event.Account;
    public override User User => Event.CreatedBy ?? throw new NullReferenceException("created_by is null");
    public override Chat Chat => Event.GetChat() ?? throw new NullReferenceException("chat is null");
    public override Message Message => Event.GetMessage() ?? throw new NullReferenceException("message is null");

    protected GithubService Github { get; } = provider.GetRequiredService<GithubService>();
}