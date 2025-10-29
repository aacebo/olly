using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public interface IJobStorage
{
    Task<Job?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Job>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Job> Create(Job value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Job> Update(Job value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class JobStorage(ILogger<IJobStorage> logger, QueryFactory db) : IJobStorage
{
    public async Task<Job?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("jobs")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Job?>(cancellationToken: cancellationToken);
    }

    public async Task<PaginationResult<Job>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByTenantId");
        page ??= new();
        var query = db
            .Query("jobs")
            .Select("*")
            .Where("tenant_id", "=", tenantId);

        return await page.Invoke<Job>(query, cancellationToken);
    }

    public async Task<IEnumerable<Job>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByParentId");
        return await db
            .Query("jobs")
            .Select("*")
            .Where("parent_id", "=", parentId)
            .GetAsync<Job>(cancellationToken: cancellationToken);
    }

    public async Task<Job> Create(Job value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO jobs
            (id, tenant_id, parent_id, name, status, message, entities, started_at, ended_at, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.TenantId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ParentId is null ? DBNull.Value : value.ParentId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Status.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Message is null ? DBNull.Value : value.Message, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Entities, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.StartedAt is null ? DBNull.Value : value.StartedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.EndedAt is null ? DBNull.Value : value.EndedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Job> Update(Job value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE jobs SET
                tenant_id = $2,
                parent_id = $3,
                name = $4,
                status = $5,
                message = $6,
                entities = $7,
                started_at = $8,
                ended_at = $9,
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
                new() { Value = value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Status.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Message is null ? DBNull.Value : value.Message, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Entities, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.StartedAt is null ? DBNull.Value : value.StartedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.EndedAt is null ? DBNull.Value : value.EndedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }
}