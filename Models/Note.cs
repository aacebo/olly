using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Models;

[Model]
public class Note : Model
{
    [Column("text")]
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [Column("data")]
    [JsonPropertyName("data")]
    public Data Data { get; set; } = new Data();

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}