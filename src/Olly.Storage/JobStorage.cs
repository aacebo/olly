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
    Task<IEnumerable<Job>> GetBlocking(Guid chatId, CancellationToken cancellationToken = default);

    Task<Job> Create(Job value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Job> Update(Job value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);

    Task AddChat(Guid chatId, Guid jobId, bool async = false, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task DelChat(Guid chatId, Guid jobId, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
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
            .Query("chats_jobs")
            .Select("jobs.*")
            .LeftJoin("jobs", "jobs.id", "chats_jobs.job_id")
            .Where("chats_jobs.chat_id", "=", chatId);

        return await page.Invoke<Job>(query, cancellationToken);
    }

    public async Task<IEnumerable<Job>> GetBlocking(Guid chatId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBlocking");
        return await db
            .Query("chats_jobs")
            .Select("jobs.*")
            .LeftJoin("jobs", join =>
                join.On("jobs.id", "chats_jobs.job_id")
                    .WhereNull("jobs.ended_at")
            )
            .Where("chats_jobs.chat_id", "=", chatId)
            .Where("chats_jobs.is_async", "=", false)
            .GetAsync<Job>(cancellationToken: cancellationToken);
    }

    public async Task<Job> Create(Job value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO jobs
            (id, install_id, parent_id, name, status, message, entities, started_at, ended_at, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.InstallId, NpgsqlDbType = NpgsqlDbType.Uuid },
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
                install_id = $2,
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
                new() { Value = value.InstallId, NpgsqlDbType = NpgsqlDbType.Uuid },
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

    public async Task AddChat(Guid chatId, Guid jobId, bool async = false, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("AddChat");
        await db.Query("chats_jobs").InsertAsync(new
        {
            chat_id = chatId,
            job_id = jobId,
            is_async = async,
            created_at = DateTimeOffset.UtcNow
        }, tx, cancellationToken: cancellationToken);
    }

    public async Task DelChat(Guid chatId, Guid jobId, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("DelChat");
        await db
            .Query("chats_jobs")
            .Where("chat_id", "=", chatId)
            .Where("job_id", "=", jobId)
            .DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}