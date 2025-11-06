using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using Olly.Storage.Models;

using SqlKata.Execution;

namespace Olly.Storage;

public interface ILogStorage
{
    Task<Log?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Log>> GetByTypeId(Guid tenantId, LogType type, string typeId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Log>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default);
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

    public async Task<PaginationResult<Log>> GetByTypeId(Guid tenantId, LogType type, string typeId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByTypeId");

        page ??= new();
        page.Sort ??= Sort.Create("created_at").Direction(SortDirection.Desc).Build();

        var query = db
            .Query("logs")
            .Select("*")
            .Where("tenant_id", "=", tenantId)
            .Where("type", "=", type.ToString())
            .Where("type_id", "=", typeId);

        return await page.Invoke<Log>(query, cancellationToken);
    }

    public async Task<PaginationResult<Log>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByTenantId");

        page ??= new();
        page.Sort ??= Sort.Create("created_at").Direction(SortDirection.Desc).Build();

        var query = db
            .Query("logs")
            .Select("*")
            .Where("tenant_id", "=", tenantId);

        return await page.Invoke<Log>(query, cancellationToken);
    }

    public async Task<Log> Create(Log value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO logs
            (id, tenant_id, level, type, type_id, text, entities, created_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.TenantId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Level.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Type.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.TypeId is null ? DBNull.Value : value.TypeId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Text, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Entities, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }
}