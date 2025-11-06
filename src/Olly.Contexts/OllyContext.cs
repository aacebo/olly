using System.Text.Json.Serialization;

namespace Olly.Contexts;

public class OllyContext
{
    [JsonPropertyName("trace_id")]
    public string TraceId { get; set; } = Guid.NewGuid().ToString();
}