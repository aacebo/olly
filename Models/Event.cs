using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OS.Agent.Models;

[Table("events")]
public class Event<T>(string name)
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("name")]
    [JsonPropertyName("name")]
    public string Name { get; init; } = name;

    [Column("body")]
    [JsonPropertyName("body")]
    public T? Body { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonExtensionData]
    public IDictionary<string, JsonElement> Extra { get; set; } = new Dictionary<string, JsonElement>();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}