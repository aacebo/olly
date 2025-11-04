using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using Olly.Storage.Models;

using SqlKata.Execution;

namespace Olly.Storage;

public interface IJobStorage
{
    Task<Job?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Job>> GetByInstallId(Guid installId, Page? page = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<PaginationResult<Job>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Job>> GetByMessageId(Guid messageId, Page? page = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> GetBlockingByChatId(Guid chatId, CancellationToken cancellationToken = default);

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

    public async Task<PaginationResult<Job>> GetByInstallId(Guid installId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByInstallId");
        page ??= new();
        var query = db
            .Query("jobs")
            .Select("*")
            .Where("install_id", "=", installId);

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

    public async Task<PaginationResult<Job>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByChatId");
        page ??= new();
        var query = db
            .Query("jobs")
            .Select("*")
            .Where("chat_id", "=", chatId);

        return await page.Invoke<Job>(query, cancellationToken);
    }

    public async Task<PaginationResult<Job>> GetByMessageId(Guid messageId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByMessageId");
        page ??= new();
        var query = db
            .Query("jobs")
            .Select("*")
            .Where("message_id", "=", messageId);

        return await page.Invoke<Job>(query, cancellationToken);
    }

    public async Task<IEnumerable<Job>> GetBlockingByChatId(Guid chatId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBlockingByChatId");
        return await db
            .Query("jobs")
            .Select("*")
            .Where("chat_id", "=", chatId)
            .Where("type", "=", JobType.Sync.ToString())
            .WhereNull("ended_at")
            .GetAsync<Job>(cancellationToken: cancellationToken);
    }

    public async Task<Job> Create(Job value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO jobs
            (id, install_id, parent_id, chat_id, message_id, type, name, title, status, message, entities, started_at, ended_at, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.InstallId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ParentId is null ? DBNull.Value : value.ParentId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ChatId is null ? DBNull.Value : value.ChatId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.MessageId is null ? DBNull.Value : value.MessageId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Type.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Title, NpgsqlDbType = NpgsqlDbType.Text },
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
                install_id = $2,
                parent_id = $3,
                chat_id = $4,
                message_id = $5,
                type = $6,
                name = $7,
                title = $8,
                status = $9,
                message = $10,
                entities = $11,
                started_at = $12,
                ended_at = $13,
                created_at = $14,
                updated_at = $15
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.InstallId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ParentId is null ? DBNull.Value : value.ParentId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ChatId is null ? DBNull.Value : value.ChatId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.MessageId is null ? DBNull.Value : value.MessageId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Type.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Title, NpgsqlDbType = NpgsqlDbType.Text },
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