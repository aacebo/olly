using System.Text.Json;
using System.Text.Json.Serialization;

namespace OS.Agent.Events;

public class Event<T>(string name)
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; init; } = name;

    [JsonPropertyName("body")]
    public T? Body { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonExtensionData]
    public IDictionary<string, JsonElement> Extra { get; set; } = new Dictionary<string, JsonElement>();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}