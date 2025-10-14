using System.Data;

using OS.Agent.Models;

using SqlKata.Execution;

namespace OS.Agent.Stores;

public interface ILogStorage
{
    Task<Log?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Log?> GetByTypeId(Guid tenantId, LogType type, string typeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Log>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Log> Create(Log value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class LogStorage(ILogger<ILogStorage> logger, QueryFactory db) : ILogStorage
{
    public async Task<Log?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("logs")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Log?>(cancellationToken: cancellationToken);
    }

    public async Task<Log?> GetByTypeId(Guid tenantId, LogType type, string typeId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByTypeId");
        return await db
            .Query("logs")
            .Select("*")
            .Where("tenant_id", "=", tenantId)
            .Where("type", "=", type.ToString())
            .Where("type_id", "=", typeId)
            .FirstOrDefaultAsync<Log?>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Log>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByTenantId");
        return await db
            .Query()
            .Select("*")
            .From("logs")
            .Where("tenant_id", "=", tenantId)
            .GetAsync<Log>(cancellationToken: cancellationToken);
    }

    public async Task<Log> Create(Log value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        await db.Query("logs").InsertAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }
}