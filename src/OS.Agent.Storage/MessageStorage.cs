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
    Task<IEnumerable<Message>> GetByChatId(Guid chatId, CancellationToken cancellationToken = default);
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

    public async Task<IEnumerable<Message>> GetByChatId(Guid chatId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByChatId");
        return await db
            .Query("messages")
            .Select("*")
            .Where("chat_id", "=", chatId)
            .GetAsync<Message>(cancellationToken: cancellationToken);
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
            (id, chat_id, account_id, source_id, source_type, text, data, notes, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ChatId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId is null ? DBNull.Value : value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Text, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Data, NpgsqlDbType = NpgsqlDbType.Jsonb },
                new() { Value = value.Notes, NpgsqlDbType = NpgsqlDbType.Jsonb },
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
                source_id = $4,
                source_type = $5,
                text = $6,
                data = $7,
                notes = $8,
                created_at = $9,
                updated_at = $10
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.ChatId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId is null ? DBNull.Value : value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.SourceId, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.SourceType.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Text, NpgsqlDbType = NpgsqlDbType.Text },
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
        await db.Query("messages").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}