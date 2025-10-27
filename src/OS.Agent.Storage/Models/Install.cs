using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Storage.Models;

[Model]
public class Install : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("account_id")]
    [JsonPropertyName("account_id")]
    public required Guid AccountId { get; init; }

    [Column("source_id")]
    [JsonPropertyName("source_id")]
    public required string SourceId { get; set; }

    [Column("source_type")]
    [JsonPropertyName("source_type")]
    public required SourceType SourceType { get; init; }

    [Column("url")]
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [Column("access_token")]
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [Column("expires_at")]
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [Column("entities")]
    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Install Copy()
    {
        return (Install)MemberwiseClone();
    }
}