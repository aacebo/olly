using System.Data;

using OS.Agent.Models;

using SqlKata.Execution;

namespace OS.Agent.Stores;

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
        await db.Query("messages").InsertAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task<Message> Update(Message value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        await db.Query("messages").Where("id", "=", value.Id).UpdateAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Delete");
        await db.Query("messages").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}