using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;

using OS.Agent.Drivers;
using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Prompts;

public interface IPromptContext
{
    Guid UserId { get; }
    OpenAIChatModel Model { get; }
    Tenant Tenant { get; }
    Account Account { get; }
    Chat Chat { get; }
    Message Message { get; }
    ITenantService Tenants { get; }
    IAccountService Accounts { get; }
    IChatService Chats { get; }
    IMessageService Messages { get; }
    ITokenService Tokens { get; }
    IChatDriver Driver { get; }
    CancellationToken CancellationToken { get; }
    IServiceScope Scope { get; }

    Task Send(IActivity activity, CancellationToken cancellationToken = default);
}

public class PromptContext : IPromptContext
{
    public Guid UserId => Account.UserId ?? throw new InvalidDataException("Accounts sending messages must be assigned to a user!");
    public OpenAIChatModel Model { get; }
    public Tenant Tenant { get; set; }
    public Account Account { get; set; }
    public Chat Chat { get; set; }
    public Message Message { get; set; }
    public ITenantService Tenants { get; }
    public IAccountService Accounts { get; }
    public IChatService Chats { get; }
    public IMessageService Messages { get; }
    public ITokenService Tokens { get; }
    public IChatDriver Driver { get; }
    public IStorage Storage { get; }
    public CancellationToken CancellationToken { get; }
    public IServiceScope Scope { get; }

    public PromptContext(MessageEvent @event, IServiceScope scope, CancellationToken cancellationToken = default)
    {
        Tenants = scope.ServiceProvider.GetRequiredService<ITenantService>();
        Accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        Chats = scope.ServiceProvider.GetRequiredService<IChatService>();
        Messages = scope.ServiceProvider.GetRequiredService<IMessageService>();
        Tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        Model = scope.ServiceProvider.GetRequiredService<OpenAIChatModel>();
        Driver = scope.ServiceProvider.GetServices<IChatDriver>().First(driver => driver.Type == @event.Message.SourceType);
        Storage = scope.ServiceProvider.GetRequiredService<IStorage>();
        Tenant = @event.Tenant;
        Account = @event.Account;
        Chat = @event.Chat;
        Message = @event.Message;
        CancellationToken = cancellationToken;
        Scope = scope;
    }

    public async Task Send(IActivity activity, CancellationToken cancellationToken = default)
    {
        activity.Conversation = new()
        {
            Id = Chat.SourceId,
            Type = Chat.Type is not null ? new(Chat.Type) : new("personal"),
            Name = Chat.Name
        };

        var res = await Driver.Send(activity, cancellationToken);

        if (res is MessageActivity message)
        {
            if (string.IsNullOrEmpty(message.Id)) return;

            await Storage.Messages.Create(new()
            {
                ChatId = Chat.Id,
                SourceType = Message.SourceType,
                SourceId = message.Id,
                Text = message.Text,
                Data = new TeamsMessageData()
                {
                    Activity = message
                }
            }, cancellationToken: cancellationToken);
        }
    }
}