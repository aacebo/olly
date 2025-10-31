using Microsoft.Extensions.DependencyInjection;

using Olly.Events;
using Olly.Storage.Models;

namespace Olly.Drivers.Github;

public partial class GithubClient : Client
{
    public override Tenant Tenant { get; }
    public override Account Account { get; }
    public override User? User { get; }
    public override Install Install { get; }
    public override Chat Chat { get; }
    public override Message? Message { get; }

    protected Event Event { get; }
    protected GithubService Github { get; }

    public GithubClient(InstallEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default) : base(provider, cancellationToken)
    {
        Event = @event;
        Tenant = @event.Tenant;
        Account = @event.Account;
        User = @event.CreatedBy;
        Install = @event.Install;
        Chat = @event.Chat ?? throw new Exception("install event must have a chat");
        Message = @event.Message;
        Github = provider.GetRequiredService<GithubService>();
    }

    public GithubClient(MessageEvent @event, IServiceProvider provider, CancellationToken cancellationToken = default) : base(provider, cancellationToken)
    {
        Event = @event;
        Tenant = @event.Tenant;
        Account = @event.Account;
        User = @event.CreatedBy;
        Install = @event.Install;
        Chat = @event.Chat;
        Message = @event.Message;
        Github = provider.GetRequiredService<GithubService>();
    }
}