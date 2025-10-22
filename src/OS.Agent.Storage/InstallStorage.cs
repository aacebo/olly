using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public interface IInstallStorage
{
    Task<Install?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Install>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default);
    Task<Install?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<Install> Create(Install value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Install> Update(Install value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class InstallStorage(ILogger<IInstallStorage> logger, QueryFactory db) : IInstallStorage
{
    public async Task<Install?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("installs")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Install?>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Install>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByAccountId");
        return await db
            .Query()
            .Select("*")
            .From("installs")
            .Where("account_id", "=", accountId)
            .GetAsync<Install>(cancellationToken: cancellationToken);
    }

    public async Task<Install?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("installs")
            .Select("*")
            .Where("source_type", "=", type.ToString())
            .Where("source_id", "=", sourceId)
            .FirstOrDefaultAsync<Install?>(cancellationToken: cancellationToken);
    }

    public async Task<Install> Create(Install value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO installs
            (id, account_id, source_id, source_type, access_token, expires_at, entities, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.AccessToken is null ? DBNull.Value : value.AccessToken, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.ExpiresAt is null ? DBNull.Value : value.ExpiresAt, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Entities, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Install> Update(Install value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE installs SET
                account_id = $2,
                source_id = $3,
                source_type = $4,
                access_token = $5,
                expires_at = $6,
                entities = $7,
                created_at = $8,
                updated_at = $9
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.AccessToken is null ? DBNull.Value : value.AccessToken, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.ExpiresAt is null ? DBNull.Value : value.ExpiresAt, NpgsqlDbType = NpgsqlDbType.Text },
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
        await db.Query("installs").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}