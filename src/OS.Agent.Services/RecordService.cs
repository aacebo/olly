using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Services;

public interface IRecordService
{
    Task<Record?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Record>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Record>> GetByAccountId(Guid accountId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Record>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Record>> GetByMessageId(Guid messageId, Page? page = null, CancellationToken cancellationToken = default);
    Task<Record?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Record>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Record> Create(Record value, CancellationToken cancellationToken = default);
    Task<Record> Create(Tenant tenant, Record value, CancellationToken cancellationToken = default);
    Task<Record> Create(Account account, Record value, CancellationToken cancellationToken = default);
    Task<Record> Create(Chat chat, Record value, CancellationToken cancellationToken = default);
    Task<Record> Create(Message message, Record value, CancellationToken cancellationToken = default);
    Task<Record> Update(Record value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class RecordService(IServiceProvider provider) : IRecordService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<RecordEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<RecordEvent>>();
    private IRecordStorage Storage { get; init; } = provider.GetRequiredService<IRecordStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();
    private IAccountService Accounts { get; init; } = provider.GetRequiredService<IAccountService>();
    private IChatService Chats { get; init; } = provider.GetRequiredService<IChatService>();
    private IUserService Users { get; init; } = provider.GetRequiredService<IUserService>();

    public async Task<Record?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var record = Cache.Get<Record>(id);

        if (record is not null)
        {
            return record;
        }

        record = await Storage.GetById(id, cancellationToken);

        if (record is not null)
        {
            Cache.Set(record.Id, record);
        }

        return record;
    }

    public async Task<PaginationResult<Record>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByTenantId(tenantId, page, cancellationToken);
    }

    public async Task<PaginationResult<Record>> GetByAccountId(Guid accountId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByAccountId(accountId, page, cancellationToken);
    }

    public async Task<PaginationResult<Record>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByChatId(chatId, page, cancellationToken);
    }

    public async Task<PaginationResult<Record>> GetByMessageId(Guid messageId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByMessageId(messageId, page, cancellationToken);
    }

    public async Task<Record?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        var record = await Storage.GetBySourceId(type, sourceId, cancellationToken);

        if (record is not null)
        {
            Cache.Set(record.Id, record);
        }

        return record;
    }

    public async Task<IEnumerable<Record>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByParentId(parentId, cancellationToken);
    }

    public async Task<Record> Create(Record value, CancellationToken cancellationToken = default)
    {
        var record = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Record = record
        });

        return record;
    }

    public async Task<Record> Create(Tenant tenant, Record value, CancellationToken cancellationToken = default)
    {
        var record = await Storage.Create(tenant, value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Record = record
        });

        return record;
    }

    public async Task<Record> Create(Account account, Record value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = account.UserId is not null ? await Users.GetById(account.UserId.Value, cancellationToken) : null;
        var record = await Storage.Create(account, value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Account = account,
            Record = record,
            CreatedBy = user
        });

        return record;
    }

    public async Task<Record> Create(Chat chat, Record value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(chat.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var record = await Storage.Create(chat, value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Chat = chat,
            Record = record
        });

        return record;
    }

    public async Task<Record> Create(Message message, Record value, CancellationToken cancellationToken = default)
    {
        var chat = await Chats.GetById(message.ChatId, cancellationToken) ?? throw new Exception("chat not found");
        var tenant = await Tenants.GetById(chat.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var account = message.AccountId is not null ? await Accounts.GetById(message.AccountId.Value, cancellationToken) : null;
        var user = account?.UserId is not null ? await Users.GetById(account.UserId.Value, cancellationToken) : null;
        var record = await Storage.Create(message, value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Account = account,
            Chat = chat,
            Message = message,
            Record = record,
            CreatedBy = user
        });

        return record;
    }

    public async Task<Record> Update(Record value, CancellationToken cancellationToken = default)
    {
        var record = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Record = record
        });

        return record;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await GetById(id, cancellationToken) ?? throw new Exception("record not found");

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Record = record
        });
    }
}