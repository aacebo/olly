using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public interface IAccountStorage
{
    Task<Account?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Account> Create(Account value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Account> Update(Account value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class AccountStorage(ILogger<IAccountStorage> logger, QueryFactory db) : IAccountStorage
{
    public async Task<Account?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("accounts")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Account?>(cancellationToken: cancellationToken);
    }

    public async Task<Account?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("accounts")
            .Select("*")
            .Where("tenant_id", "=", tenantId)
            .Where("source_type", "=", type.ToString())
            .Where("source_id", "=", sourceId)
            .FirstOrDefaultAsync<Account?>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByTenantId");
        return await db
            .Query()
            .Select("*")
            .From("accounts")
            .Where("tenant_id", "=", tenantId)
            .GetAsync<Account>(cancellationToken: cancellationToken);
    }

    public async Task<Account> Create(Account value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO accounts
            (id, tenant_id, source_id, source_type, url, name, entities, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.TenantId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url is null ? DBNull.Value : value.Url, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Name is null ? DBNull.Value : value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Entities, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Account> Update(Account value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE accounts SET
                tenant_id = $2,
                source_id = $3,
                source_type = $4,
                url = $5,
                name = $6,
                entities = $7,
                created_at = $8,
                updated_at = $9
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.TenantId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url is null ? DBNull.Value : value.Url, NpgsqlDbType = NpgsqlDbType.Text },
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
        await db.Query("accounts").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}