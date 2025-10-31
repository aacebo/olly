using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using Olly.Storage.Models;

using SqlKata.Execution;

namespace Olly.Storage;

public interface IInstallStorage
{
    Task<Install?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Install>> GetByUserId(Guid userId, Query? query = null, CancellationToken cancellationToken = default);
    Task<Install?> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default);
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

    public async Task<IEnumerable<Install>> GetByUserId(Guid userId, Query? query = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByUserId");
        var q = db
            .Query("installs")
            .Select("*")
            .Where("user_id", "=", userId);

        query ??= new();
        return await query.Invoke<Install>(q, cancellationToken);
    }

    public async Task<Install?> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByAccountId");
        return await db
            .Query("installs")
            .Select("*")
            .Where("account_id", "=", accountId)
            .FirstOrDefaultAsync<Install?>(cancellationToken: cancellationToken);
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
            (id, user_id, account_id, message_id, source_id, source_type, status, url, access_token, expires_at, entities, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.UserId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.MessageId is null ? DBNull.Value : value.MessageId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Status.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url is null ? DBNull.Value : value.Url, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.AccessToken is null ? DBNull.Value : value.AccessToken, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.ExpiresAt is null ? DBNull.Value : value.ExpiresAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
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
                user_id = $2,
                account_id = $3,
                message_id = $4,
                source_id = $5,
                source_type = $6,
                status = $7,
                url = $8,
                access_token = $9,
                expires_at = $10,
                entities = $11,
                created_at = $12,
                updated_at = $13
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.UserId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.MessageId is null ? DBNull.Value : value.MessageId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Status.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url is null ? DBNull.Value : value.Url, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.AccessToken is null ? DBNull.Value : value.AccessToken, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.ExpiresAt is null ? DBNull.Value : value.ExpiresAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
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