using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using Olly.Storage.Models.Jobs;

using SqlKata.Execution;

namespace Olly.Storage;

public interface IJobApprovalStorage
{
    Task<Approval?> GetOne(Guid jobId, Guid accountId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Approval>> GetByJobId(Guid jobId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Approval>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default);
    Task<Approval> Create(Approval value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Approval> Update(Approval value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid jobId, Guid accountId, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class JobApprovalStorage(ILogger<IJobApprovalStorage> logger, QueryFactory db) : IJobApprovalStorage
{
    public async Task<Approval?> GetOne(Guid jobId, Guid accountId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetOne");
        return await db
            .Query("job_approvals")
            .Select("*")
            .Where("job_id", "=", jobId)
            .Where("account_id", "=", accountId)
            .FirstOrDefaultAsync<Approval?>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetByJobId(Guid jobId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByJobId");
        return await db
            .Query("job_approvals")
            .Select("*")
            .Where("job_id", "=", jobId)
            .GetAsync<Approval>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByAccountId");
        return await db
            .Query("job_approvals")
            .Select("*")
            .Where("account_id", "=", accountId)
            .GetAsync<Approval>(cancellationToken: cancellationToken);
    }

    public async Task<Approval> Create(Approval value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO job_approvals
            (job_id, account_id, status, required, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.JobId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Status.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Required, NpgsqlDbType = NpgsqlDbType.Boolean },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Approval> Update(Approval value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE job_approvals SET
                status = $3,
                updated_at = $4
            WHERE job_id = $1
            AND account_id = $2
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.JobId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.AccountId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Status.ToString(), NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task Delete(Guid jobId, Guid accountId, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Delete");
        await db.Query("job_approvals")
            .Where("job_id", "=", jobId)
            .Where("account_id", "=", accountId)
            .DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}