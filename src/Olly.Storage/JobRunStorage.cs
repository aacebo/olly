using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using Olly.Storage.Models.Jobs;

using SqlKata.Execution;

namespace Olly.Storage;

public interface IJobRunStorage
{
    Task<Run?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Run>> GetByJobId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default);
    Task<Run> Create(Run value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Run> Update(Run value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class JobRunStorage(ILogger<IJobRunStorage> logger, QueryFactory db) : IJobRunStorage
{
    public async Task<Run?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("job_runs")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Run?>(cancellationToken: cancellationToken);
    }

    public async Task<PaginationResult<Run>> GetByJobId(Guid jobId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByJobId");
        page ??= new();
        var query = db
            .Query("job_runs")
            .Select("*")
            .Where("job_id", "=", jobId);

        return await page.Invoke<Run>(query, cancellationToken);
    }

    public async Task<Run> Create(Run value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO job_runs
            (id, job_id, status, status_message, started_at, ended_at, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.JobId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Status.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.StatusMessage is null ? DBNull.Value : value.StatusMessage, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.StartedAt is null ? DBNull.Value : value.StartedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.EndedAt is null ? DBNull.Value : value.EndedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Run> Update(Run value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE job_runs SET
                job_id = $2,
                status = $3,
                status_message = $4,
                started_at = $5,
                ended_at = $6,
                created_at = $7,
                updated_at = $8
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.JobId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Status.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.StatusMessage is null ? DBNull.Value : value.StatusMessage, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.StartedAt is null ? DBNull.Value : value.StartedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.EndedAt is null ? DBNull.Value : value.EndedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
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
        await db.Query("job_runs").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}