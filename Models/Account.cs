using System.Text.Json;
using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Models;

public class Account
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("user_id")]
    [JsonPropertyName("user_id")]
    public required Guid UserId { get; init; }

    [Column("external_id")]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    [Column("type")]
    [JsonPropertyName("type")]
    public required AccountType Type { get; init; }

    [Column("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [Column("data")]
    [JsonPropertyName("data")]
    public JsonDocument? Data { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}