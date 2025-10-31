using System.Text.Json.Serialization;

using Pgvector;

using SqlKata;

namespace Olly.Storage.Models;

[Model]
public class Document : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("record_id")]
    [JsonPropertyName("record_id")]
    public required Guid RecordId { get; init; }

    [Column("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [Column("path")]
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    [Column("url")]
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [Column("size")]
    [JsonPropertyName("size")]
    public required long Size { get; set; }

    [Column("encoding")]
    [JsonPropertyName("encoding")]
    public string? Encoding { get; set; }

    [Column("content")]
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [Column("embedding")]
    [JsonPropertyName("embedding")]
    public Vector? Embedding { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Document Copy()
    {
        return (Document)MemberwiseClone();
    }
}