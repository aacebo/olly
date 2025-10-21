using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public interface IMessageStorage
{
    Task<Message?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Message>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Message>> GetByParentId(Guid id, Page? page = null, CancellationToken cancellationToken = default);
    Task<Message?> GetBySourceId(Guid chatId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<Message> Create(Message value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Message> Update(Message value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class MessageStorage(ILogger<IMessageStorage> logger, QueryFactory db) : IMessageStorage
{
    public async Task<Message?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("messages")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Message?>(cancellationToken: cancellationToken);
    }

    public async Task<PaginationResult<Message>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByChatId");
        page ??= new();
        var query = db
            .Query("messages")
            .Select("*")
            .Where("chat_id", "=", chatId);

        return await page.Invoke<Message>(query, cancellationToken);
    }

    public async Task<PaginationResult<Message>> GetByParentId(Guid id, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByParentId");
        page ??= new();
        var query = db
            .Query("messages")
            .Select("*")
            .Where("reply_to_id", "=", id);

        return await page.Invoke<Message>(query, cancellationToken);
    }

    public async Task<Message?> GetBySourceId(Guid chatId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("messages")
            .Select("*")
            .Where("chat_id", "=", chatId)
            .Where("source_type", "=", type.ToString())
            .Where("source_id", "=", sourceId)
            .FirstOrDefaultAsync<Message?>(cancellationToken: cancellationToken);
    }

    public async Task<Message> Create(Message value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO messages
            (id, chat_id, account_id, reply_to_id, source_id, source_type, url, text, attachments, entities, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ChatId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId is null ? DBNull.Value : value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ReplyToId is null ? DBNull.Value : value.ReplyToId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url is null ? DBNull.Value : value.Url, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Text, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Attachments, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.Entities, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Message> Update(Message value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE messages Set
                chat_id = $2,
                account_id = $3,
                reply_to_id = $4,
                source_id = $5,
                source_type = $6,
                url = $7,
                text = $8,
                attachments = $9,
                entities = $10,
                created_at = $11,
                updated_at = $12
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ChatId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId is null ? DBNull.Value : value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ReplyToId is null ? DBNull.Value : value.ReplyToId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url is null ? DBNull.Value : value.Url, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Text, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Attachments, NpgsqlDbType = NpgsqlDbType.Jsonb },
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
        await db.Query("messages").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}