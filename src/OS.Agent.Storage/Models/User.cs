using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Storage.Models;

[Model]
public class User : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User Copy()
    {
        return (User)MemberwiseClone();
    }
}