using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Services;

public interface ILogService
{
    Task<Log?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Log?> GetByTypeId(Guid tenantId, LogType type, string typeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Log> Create(Log value, CancellationToken cancellationToken = default);
}

public class LogService(IServiceProvider provider) : ILogService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<LogEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<LogEvent>>();
    private ILogStorage Storage { get; init; } = provider.GetRequiredService<ILogStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();
    private IAccountService Accounts { get; init; } = provider.GetRequiredService<IAccountService>();

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

    public async Task<Log?> GetByTypeId(Guid tenantId, LogType type, string typeId, CancellationToken cancellationToken = default)
    {
        var log = await Storage.GetByTypeId(tenantId, type, typeId, cancellationToken);

        if (log is not null)
        {
            Cache.Set(log.Id, log);
        }

        return log;
    }

    public async Task<IEnumerable<Log>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByTenantId(tenantId, cancellationToken);
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