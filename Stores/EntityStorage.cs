using System.Data;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Models;

using SqlKata.Execution;

namespace OS.Agent.Stores;

public interface IEntityStorage
{
    Task<Entity?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Entity?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Entity>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Entity> Create(Entity value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Entity> Update(Entity value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class EntityStorage(ILogger<IEntityStorage> logger, QueryFactory db) : IEntityStorage
{
    public async Task<Entity?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("entities")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Entity?>(cancellationToken: cancellationToken);
    }

    public async Task<Entity?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("entities")
            .Select("*")
            .Where("tenant_id", "=", tenantId)
            .Where("source_type", "=", type.ToString())
            .Where("source_id", "=", sourceId)
            .FirstOrDefaultAsync<Entity?>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Entity>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByParentId");
        return await db
            .Query("entities")
            .Select("*")
            .Where("parent_id", "=", parentId)
            .GetAsync<Entity>(cancellationToken: cancellationToken);
    }

    public async Task<Entity> Create(Entity value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO entities
            (id, tenant_id, account_id, parent_id, source_id, source_type, type, name, data, notes, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.TenantId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId is null ? DBNull.Value : value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ParentId is null ? DBNull.Value : value.ParentId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Type, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Name is null ? DBNull.Value : value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Data, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.Notes, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Entity> Update(Entity value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE entities SET
                id = $2,
                tenant_id = $3,
                account_id = $4,
                parent_id = $5,
                source_id = $6,
                source_type = $7,
                type = $8,
                name = $9,
                data = $10,
                notes = $11,
                created_at = $12,
                updated_at = $13
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.TenantId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId is null ? DBNull.Value : value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ParentId is null ? DBNull.Value : value.ParentId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Type, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Name is null ? DBNull.Value : value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Data, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.Notes, NpgsqlDbType = NpgsqlDbType.Jsonb },
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
        await db.Query("entities").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}