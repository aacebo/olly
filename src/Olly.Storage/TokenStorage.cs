using System.Data;

using Microsoft.Extensions.Logging;

using Olly.Storage.Models;

using SqlKata.Execution;

namespace Olly.Storage;

public interface ITokenStorage
{
    Task<Token?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Token?> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default);
    Task<Token> Create(Token value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Token> Update(Token value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class TokenStorage(ILogger<ITokenStorage> logger, QueryFactory db) : ITokenStorage
{
    public async Task<Token?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("tokens")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Token?>(cancellationToken: cancellationToken);
    }

    public async Task<Token?> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByAccountId");
        return await db
            .Query("tokens")
            .Select("*")
            .Where("account_id", "=", accountId)
            .FirstOrDefaultAsync<Token?>(cancellationToken: cancellationToken);
    }

    public async Task<Token> Create(Token value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        await db.Query("tokens").InsertAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task<Token> Update(Token value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        await db.Query("tokens").Where("id", "=", value.Id).UpdateAsync(value, tx, cancellationToken: cancellationToken);
        return value;
    }

    public async Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Delete");
        await db.Query("tokens").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}