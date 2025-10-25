using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class MessageUpdateRequest : DriverRequest
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attachments")]
    public IList<Attachment>? Attachments { get; set; }

    [JsonPropertyName("from")]
    public required Account From { get; set; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; set; }

    [JsonPropertyName("install")]
    public required Install Install { get; set; }

    [JsonPropertyName("message")]
    public required Message Message { get; set; }
}