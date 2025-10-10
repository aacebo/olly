using System.Text.Json;
using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Models;

public class Chat
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

    [Column("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [Column("data")]
    [JsonPropertyName("data")]
    public JsonDocument Data { get; set; } = JsonDocument.Parse("{}");

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}