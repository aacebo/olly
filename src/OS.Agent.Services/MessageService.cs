using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Services;

public interface IMessageService
{
    Task<Message?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Message>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Message>> GetByParentId(Guid id, Page? page = null, CancellationToken cancellationToken = default);
    Task<Message?> GetBySourceId(Guid chatId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<Message> Create(Message value, CancellationToken cancellationToken = default);
    Task<Message> Update(Message value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
    Task Resume(Guid id, CancellationToken cancellationToken = default);
}

public class MessageService(IServiceProvider provider) : IMessageService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<MessageEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<MessageEvent>>();
    private IMessageStorage Storage { get; init; } = provider.GetRequiredService<IMessageStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();
    private IAccountService Accounts { get; init; } = provider.GetRequiredService<IAccountService>();
    private IInstallService Installs { get; init; } = provider.GetRequiredService<IInstallService>();
    private IChatService Chats { get; init; } = provider.GetRequiredService<IChatService>();

    public async Task<Message?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var message = Cache.Get<Message>(id);

        if (message is not null)
        {
            return message;
        }

        message = await Storage.GetById(id, cancellationToken);

        if (message is not null)
        {
            Cache.Set(message.Id, message);
        }

        return message;
    }

    public async Task<PaginationResult<Message>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByChatId(chatId, page, cancellationToken);
    }

    public async Task<PaginationResult<Message>> GetByParentId(Guid id, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByParentId(id, page, cancellationToken);
    }

    public async Task<Message?> GetBySourceId(Guid chatId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        var message = await Storage.GetBySourceId(chatId, type, sourceId, cancellationToken);

        if (message is not null)
        {
            Cache.Set(message.Id, message);
        }

        return message;
    }

    public async Task<Message> Create(Message value, CancellationToken cancellationToken = default)
    {
        if (value.AccountId is null) throw new UnauthorizedAccessException();

        var account = await Accounts.GetById(value.AccountId.Value, cancellationToken) ?? throw new Exception("account not found");
        var install = await Installs.GetByAccountId(account.Id, cancellationToken) ?? throw new Exception("install not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var chat = await Chats.GetById(value.ChatId, cancellationToken) ?? throw new Exception("chat not found");
        var message = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            Chat = chat,
            Message = message
        });

        return message;
    }

    public async Task<Message> Update(Message value, CancellationToken cancellationToken = default)
    {
        if (value.AccountId is null) throw new UnauthorizedAccessException();

        var account = await Accounts.GetById(value.AccountId.Value, cancellationToken) ?? throw new Exception("account not found");
        var install = await Installs.GetByAccountId(account.Id, cancellationToken) ?? throw new Exception("install not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var chat = await Chats.GetById(value.ChatId, cancellationToken) ?? throw new Exception("chat not found");
        var message = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            Chat = chat,
            Message = message
        });

        return message;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await GetById(id, cancellationToken) ?? throw new Exception("message not found");

        if (message.AccountId is null) throw new UnauthorizedAccessException();

        var account = await Accounts.GetById(message.AccountId.Value, cancellationToken) ?? throw new Exception("account not found");
        var install = await Installs.GetByAccountId(account.Id, cancellationToken) ?? throw new Exception("install not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var chat = await Chats.GetById(message.ChatId, cancellationToken) ?? throw new Exception("chat not found");

        await Storage.Delete(message.Id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            Chat = chat,
            Message = message
        });
    }

    public async Task Resume(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await GetById(id, cancellationToken) ?? throw new Exception("message not found");

        if (message.AccountId is null) throw new UnauthorizedAccessException();

        var account = await Accounts.GetById(message.AccountId.Value, cancellationToken) ?? throw new Exception("account not found");
        var install = await Installs.GetByAccountId(account.Id, cancellationToken) ?? throw new Exception("install not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var chat = await Chats.GetById(message.ChatId, cancellationToken) ?? throw new Exception("chat not found");

        Events.Enqueue(new(ActionType.Resume)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            Chat = chat,
            Message = message
        });
    }
}