using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public interface IRecordStorage
{
    Task<Record?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Record>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default);
    Task<Record?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Record>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Record> Create(Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Record> Update(Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class RecordStorage(ILogger<IRecordStorage> logger, QueryFactory db) : IRecordStorage
{
    public async Task<Record?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("records")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Record?>(cancellationToken: cancellationToken);
    }

    public async Task<PaginationResult<Record>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByTenantId");
        page ??= new();
        var query = db
            .Query()
            .Select("*")
            .From("records")
            .Where("tenant_id", "=", tenantId);

        return await page.Invoke<Record>(query, cancellationToken);
    }

    public async Task<Record?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("records")
            .Select("*")
            .Where("tenant_id", "=", tenantId)
            .Where("source_type", "=", type.ToString())
            .Where("source_id", "=", sourceId)
            .FirstOrDefaultAsync<Record?>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Record>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByParentId");
        return await db
            .Query("records")
            .Select("*")
            .Where("parent_id", "=", parentId)
            .GetAsync<Record>(cancellationToken: cancellationToken);
    }

    public async Task<Record> Create(Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO records
            (id, tenant_id, parent_id, source_id, source_type, url, type, name, entities, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.TenantId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ParentId is null ? DBNull.Value : value.ParentId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url is null ? DBNull.Value : value.Url, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Type, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Name is null ? DBNull.Value : value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Entities, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Record> Update(Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE records SET
                tenant_id = $2,
                parent_id = $3,
                source_id = $4,
                source_type = $5,
                url = $6,
                type = $7,
                name = $8,
                entities = $9,
                created_at = $10,
                updated_at = $11
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.TenantId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ParentId is null ? DBNull.Value : value.ParentId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url is null ? DBNull.Value : value.Url, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Type is null ? DBNull.Value : value.Type, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Name is null ? DBNull.Value : value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Entities, NpgsqlDbType = NpgsqlDbType.Jsonb },
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
        await db.Query("records").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}