using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Storage.Models;

[Model]
public class Record : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("tenant_id")]
    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [Column("parent_id")]
    [JsonPropertyName("parent_id")]
    public Guid? ParentId { get; set; }

    [Column("source_id")]
    [JsonPropertyName("source_id")]
    public required string SourceId { get; set; }

    [Column("source_type")]
    [JsonPropertyName("source_type")]
    public required SourceType SourceType { get; init; }

    [Column("url")]
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [Column("type")]
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [Column("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [Column("entities")]
    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}