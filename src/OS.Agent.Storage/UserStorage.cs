using System.Data;

using Microsoft.Extensions.Logging;

using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public interface IUserStorage
{
    Task<User?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<User> Create(User value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<User> Update(User value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class UserStorage(ILogger<IUserStorage> logger, QueryFactory db) : IUserStorage
{
    public async Task<User?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db.Query()
            .Select("*")
            .From("users")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<User?>(cancellationToken: cancellationToken);
    }

    public async Task<User> Create(User value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        await db.Query("users").InsertAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task<User> Update(User value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        await db.Query("users").UpdateAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Delete");
        await db.Query("users").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}