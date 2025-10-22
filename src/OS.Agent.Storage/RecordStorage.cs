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
    Task<PaginationResult<Record>> GetByAccountId(Guid accountId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Record>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default);
    Task<PaginationResult<Record>> GetByMessageId(Guid messageId, Page? page = null, CancellationToken cancellationToken = default);
    Task<Record?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Record>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Record> Create(Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Record> Create(Tenant tenant, Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Record> Create(Account account, Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Record> Create(Chat chat, Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Record> Create(Message message, Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
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
            .Select("records.*")
            .From("tenants_records")
            .LeftJoin("records", "tenants_records.record_id", "records.id")
            .Where("tenants_records.tenant_id", "=", tenantId);

        return await page.Invoke<Record>(query, cancellationToken);
    }

    public async Task<PaginationResult<Record>> GetByAccountId(Guid accountId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByAccountId");
        page ??= new();
        var query = db
            .Query()
            .Select("records.*")
            .From("accounts_records")
            .LeftJoin("records", "accounts_records.record_id", "records.id")
            .Where("accounts_records.account_id", "=", accountId);

        return await page.Invoke<Record>(query, cancellationToken);
    }

    public async Task<PaginationResult<Record>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByChatId");
        page ??= new();
        var query = db
            .Query()
            .Select("records.*")
            .From("chats_records")
            .LeftJoin("records", "chats_records.record_id", "records.id")
            .Where("chats_records.chat_id", "=", chatId);

        return await page.Invoke<Record>(query, cancellationToken);
    }

    public async Task<PaginationResult<Record>> GetByMessageId(Guid messageId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByMessageId");
        page ??= new();
        var query = db
            .Query()
            .Select("records.*")
            .From("messages_records")
            .LeftJoin("records", "messages_records.record_id", "records.id")
            .Where("messages_records.message_id", "=", messageId);

        return await page.Invoke<Record>(query, cancellationToken);
    }

    public async Task<Record?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBySourceId");
        return await db
            .Query("records")
            .Select("*")
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
            (id, parent_id, source_id, source_type, url, type, name, entities, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
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

    public async Task<Record> Create(Tenant tenant, Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        value = await Create(value, tx, cancellationToken);

        await db.Query("tenants_records").InsertAsync(new
        {
            tenant_id = tenant.Id,
            record_id = value.Id,
            created_at = DateTimeOffset.UtcNow
        }, tx, cancellationToken: cancellationToken);

        return value;
    }

    public async Task<Record> Create(Account account, Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        value = await Create(value, tx, cancellationToken);

        await db.Query("accounts_records").InsertAsync(new
        {
            account_id = account.Id,
            record_id = value.Id,
            created_at = DateTimeOffset.UtcNow
        }, tx, cancellationToken: cancellationToken);

        return value;
    }

    public async Task<Record> Create(Chat chat, Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        value = await Create(value, tx, cancellationToken);

        await db.Query("chats_records").InsertAsync(new
        {
            chat_id = chat.Id,
            record_id = value.Id,
            created_at = DateTimeOffset.UtcNow
        }, tx, cancellationToken: cancellationToken);

        return value;
    }

    public async Task<Record> Create(Message message, Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        value = await Create(value, tx, cancellationToken);

        await db.Query("messages_records").InsertAsync(new
        {
            message_id = message.Id,
            record_id = value.Id,
            created_at = DateTimeOffset.UtcNow
        }, tx, cancellationToken: cancellationToken);

        return value;
    }

    public async Task<Record> Update(Record value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE records SET
                parent_id = $2,
                source_id = $3,
                source_type = $4,
                url = $5,
                type = $6,
                name = $7,
                entities = $8,
                created_at = $9,
                updated_at = $10
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
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