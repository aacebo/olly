using System.Data;

using Microsoft.Extensions.Logging;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Storage.Models;

using Pgvector;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public interface IDocumentStorage
{
    Task<Document?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Document?> GetByPath(Guid recordId, string path, CancellationToken cancellationToken = default);
    Task<PaginationResult<Document>> GetByRecordId(Guid recordId, Page? page = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> Search(float[] embedding, int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> Search(Guid recordId, float[] embedding, int limit = 10, CancellationToken cancellationToken = default);
    Task<Document> Create(Document value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task<Document> Update(Document value, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
    Task Delete(Guid id, IDbTransaction? tx = null, CancellationToken cancellationToken = default);
}

public class DocumentStorage(ILogger<IDocumentStorage> logger, QueryFactory db) : IDocumentStorage
{
    public async Task<Document?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetById");
        return await db
            .Query("documents")
            .Select("*")
            .Where("id", "=", id)
            .FirstOrDefaultAsync<Document?>(cancellationToken: cancellationToken);
    }

    public async Task<Document?> GetByPath(Guid recordId, string path, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByPath");
        return await db
            .Query("documents")
            .Select("*")
            .Where("record_id", "=", recordId)
            .Where("path", "=", path)
            .FirstOrDefaultAsync<Document?>(cancellationToken: cancellationToken);
    }

    public async Task<PaginationResult<Document>> GetByRecordId(Guid recordId, Page? page = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetByRecordId");
        page ??= new();
        var query = db
            .Query("documents")
            .Select("*")
            .Where("record_id", "=", recordId);

        return await page.Invoke<Document>(query, cancellationToken);
    }

    public async Task<IEnumerable<Document>> Search(float[] embedding, int limit = 10, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Search");
        return await db
            .Query("documents")
            .Select("*")
            .OrderByRaw("1 - (embedding <=> ?) DESC", new Vector(embedding))
            .Limit(limit)
            .GetAsync<Document>(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Document>> Search(Guid recordId, float[] embedding, int limit = 10, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Search");
        return await db
            .Query("documents")
            .Select("*")
            .Where("record_id", "=", recordId)
            .OrderByRaw("1 - (embedding <=> ?) DESC", new Vector(embedding))
            .Limit(limit)
            .GetAsync<Document>(cancellationToken: cancellationToken);
    }

    public async Task<Document> Create(Document value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Create");
        using var cmd = new NpgsqlCommand(
        """
            INSERT INTO documents
            (id, record_id, name, path, url, size, encoding, content, embedding, created_at, updated_at)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.RecordId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Name, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Path, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Size, NpgsqlDbType = NpgsqlDbType.Bigint },
                new() { Value = string.IsNullOrEmpty(value.Encoding) ? DBNull.Value : value.Encoding, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = string.IsNullOrEmpty(value.Content) ? DBNull.Value : value.Content, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Embedding is null ? DBNull.Value : value.Embedding },
                new() { Value = value.CreatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz },
                new() { Value = value.UpdatedAt, NpgsqlDbType = NpgsqlDbType.TimestampTz }
            }
        };

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return value;
    }

    public async Task<Document> Update(Document value, IDbTransaction? tx = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update");
        value.UpdatedAt = DateTimeOffset.UtcNow;
        using var cmd = new NpgsqlCommand(
        """
            UPDATE documents SET
                record_id = $2,
                name = $3,
                path = $4,
                url = $5,
                size = $6,
                encoding = $7,
                content = $8,
                embedding = $9,
                created_at = $10,
                updated_at = $11
            WHERE id = $1
        """, (NpgsqlConnection)db.Connection)
        {
            Parameters =
            {
                new() { Value = value.Id, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.RecordId, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Name, NpgsqlDbType = NpgsqlDbType.Uuid },
                new() { Value = value.Path, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Url, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Size, NpgsqlDbType = NpgsqlDbType.Bigint },
                new() { Value = string.IsNullOrEmpty(value.Encoding) ? DBNull.Value : value.Encoding, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = string.IsNullOrEmpty(value.Content) ? DBNull.Value : value.Content, NpgsqlDbType = NpgsqlDbType.Text },
                new() { Value = value.Embedding is null ? DBNull.Value : value.Embedding },
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
        await db.Query("documents").Where("id", "=", id).DeleteAsync(tx, cancellationToken: cancellationToken);
    }
}