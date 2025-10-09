using System.Text.Json;
using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Models;

public interface IEvent
{
    Guid Id { get; init; }
    string Name { get; init; }
    DateTimeOffset CreatedAt { get; init; }
}

public class Event<T>(string name, T body) : IEvent
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("name")]
    [JsonPropertyName("name")]
    public string Name { get; init; } = name;

    [Column("body")]
    [JsonPropertyName("body")]
    public T Body { get; set; } = body;

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