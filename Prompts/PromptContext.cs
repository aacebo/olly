using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

using OS.Agent.Models;
using OS.Agent.Stores;

namespace OS.Agent.Prompts;

public interface IPromptContext
{
    App App { get; }
    MessageActivity Activity { get; }
    IStorage Storage { get; }
    OpenAIChatModel Model { get; }
    Tenant Tenant { get; }
    Account Account { get; }
    Chat Chat { get; }
    CancellationToken CancellationToken { get; }

    Task<MessageActivity> Send(string message);
    Task<TActivity> Send<TActivity>(TActivity activity) where TActivity : IActivity;
}

public class PromptContext : IPromptContext
{
    public App App { get; }
    public MessageActivity Activity { get; }
    public IStorage Storage { get; }
    public OpenAIChatModel Model { get; }
    public required Tenant Tenant { get; set; }
    public required Account Account { get; set; }
    public required Chat Chat { get; set; }
    public CancellationToken CancellationToken { get; }

    private readonly IServiceScope _scope;

    public PromptContext(MessageActivity activity, IServiceScope scope, CancellationToken cancellationToken = default)
    {
        App = scope.ServiceProvider.GetRequiredService<App>();
        Activity = activity;
        Storage = scope.ServiceProvider.GetRequiredService<IStorage>();
        Model = scope.ServiceProvider.GetRequiredService<OpenAIChatModel>();
        CancellationToken = cancellationToken;
        _scope = scope;
    }

    public Task<MessageActivity> Send(string message)
    {
        return Send(new MessageActivity(message));
    }

    public async Task<TActivity> Send<TActivity>(TActivity activity) where TActivity : IActivity
    {
        return await App.Send(
            Activity.Conversation.Id,
            activity,
            Activity.Conversation.Type,
            Activity.ServiceUrl,
            CancellationToken
        );
    }
}