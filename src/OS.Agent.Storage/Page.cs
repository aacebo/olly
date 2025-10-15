using System.Text.Json.Serialization;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public class Page
{
    [JsonPropertyName("index")]
    public int Index { get; set; } = 0;

    [JsonPropertyName("size")]
    public int Size { get; set; } = 20;

    [JsonPropertyName("sort_by")]
    public string SortBy { get; set; } = "created_at";

    public Task<PaginationResult<T>> Invoke<T>(SqlKata.Query query, CancellationToken cancellationToken = default)
    {
        return query.OrderByDesc(SortBy).PaginateAsync<T>(Index + 1, Size, cancellationToken: cancellationToken);
    }
}