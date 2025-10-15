using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public interface ITenantStorage
{
    Task<Tenant?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<Tenant> Create(Tenant value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Tenant> Update(Tenant value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class TenantStorage(ILogger<ITenantStorage> logger, QueryFactory db) : ITenantStorage
{
    public async Task<Tenant?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("tenants")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Tenant?>(cancellationToken: cancellationToken);
    }

    public async Task<Tenant?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("tenants")
            .Select("id", "sources", "name", "data", "created_at", "updated_at")
            .WhereRaw("sources @> ?::JSONB", $"[{{\"type\": \"{type}\", \"id\": \"{sourceId}\"}}]")
            .FirstOrDefaultAsync<Tenant?>(cancellationToken: cancellationToken);
    }

    public async Task<Tenant> Create(Tenant value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand("INSERT INTO tenants (id, sources, name, data, created_at, updated_at) VALUES ($1, $2, $3, $4, $5, $6)", (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Sources, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.Name is null ? DBNull.Value : value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Data, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Tenant> Update(Tenant value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand("UPDATE tenants SET sources = $2, name = $3, data = $4, created_at = $5, updated_at = $6 WHERE id = $1", (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Sources, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.Name is null ? DBNull.Value : value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Data, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Delete");
        await db.Query("tenants").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}