using System.Text.Json.Serialization;

namespace OS.Agent.Storage.Models;

public class Attachment : Model
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("content_type")]
    public required string ContentType { get; set; }

    [JsonPropertyName("content")]
    public required object Content { get; set; }
}