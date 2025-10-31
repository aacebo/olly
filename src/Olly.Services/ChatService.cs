using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using Olly.Events;
using Olly.Storage;
using Olly.Storage.Models;

using SqlKata.Execution;

namespace Olly.Services;

public interface IChatService
{
    Task<Chat?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Chat>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default);
    Task<Chat?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Chat>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);

    Task<Chat> Create(Chat value, CancellationToken cancellationToken = default);
    Task<Chat> Update(Chat value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);

    Task AddRecord(Guid id, Guid recordId, CancellationToken cancellationToken = default);
    Task DelRecord(Guid id, Guid recordId, CancellationToken cancellationToken = default);
}

public class ChatService(IServiceProvider provider) : IChatService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<ChatEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<ChatEvent>>();
    private IChatStorage Storage { get; init; } = provider.GetRequiredService<IChatStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();

    public async Task<Chat?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var chat = Cache.Get<Chat>(id);

        if (chat is not null)
        {
            return chat;
        }

        chat = await Storage.GetById(id, cancellationToken);

        if (chat is not null)
        {
            Cache.Set(chat.Id, chat);
        }

        return chat;
    }

    public async Task<PaginationResult<Chat>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByTenantId(tenantId, page, cancellationToken);
    }

    public async Task<Chat?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        var chat = await Storage.GetBySourceId(tenantId, type, sourceId, cancellationToken);

        if (chat is not null)
        {
            Cache.Set(chat.Id, chat);
        }

        return chat;
    }

    public async Task<IEnumerable<Chat>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByParentId(parentId, cancellationToken);
    }

    public async Task<Chat> Create(Chat value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var chat = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Chat = chat
        });

        return chat;
    }

    public async Task<Chat> Update(Chat value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var chat = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant,
            Chat = chat
        });

        return chat;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var chat = await GetById(id, cancellationToken) ?? throw new Exception("chat not found");
        var tenant = await Tenants.GetById(chat.TenantId, cancellationToken) ?? throw new Exception("tenant not found");

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Tenant = tenant,
            Chat = chat
        });
    }

    public async Task AddRecord(Guid id, Guid recordId, CancellationToken cancellationToken = default)
    {
        await Storage.AddRecord(id, recordId, cancellationToken: cancellationToken);
    }

    public async Task DelRecord(Guid id, Guid recordId, CancellationToken cancellationToken = default)
    {
        await Storage.DelRecord(id, recordId, cancellationToken: cancellationToken);
    }
}