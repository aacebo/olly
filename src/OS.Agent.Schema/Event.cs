using System.Text.Json;
using System.Text.Json.Serialization;

namespace OS.Agent.Schema;

public class Event<T>(string name, T body)
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; init; } = name;

    [JsonPropertyName("body")]
    public T Body { get; set; } = body;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonExtensionData]
    public IDictionary<string, JsonElement> Extra { get; set; } = new Dictionary<string, JsonElement>();
}