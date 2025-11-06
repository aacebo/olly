using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using Olly.Events;
using Olly.Storage;
using Olly.Storage.Models;

using SqlKata.Execution;

namespace Olly.Services;

public interface ILogService
{
    Task<Log?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Log>> GetByTypeId(Guid tenantId, LogType type, string typeId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Log>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default);
    Task<Log> Create(Log value, CancellationToken cancellationToken = default);
}

public class LogService(IServiceProvider provider) : ILogService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<LogEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<LogEvent>>();
    private ILogStorage Storage { get; init; } = provider.GetRequiredService<ILogStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();

    public async Task<Log?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var log = Cache.Get<Log>(id);

        if (log is not null)
        {
            return log;
        }

        log = await Storage.GetById(id, cancellationToken);

        if (log is not null)
        {
            Cache.Set(log.Id, log);
        }

        return log;
    }

    public async Task<PaginationResult<Log>> GetByTypeId(Guid tenantId, LogType type, string typeId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByTypeId(tenantId, type, typeId, page, cancellationToken);
    }

    public async Task<PaginationResult<Log>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByTenantId(tenantId, page, cancellationToken);
    }

    public async Task<Log> Create(Log value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var log = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Log = log
        });

        return log;
    }
}