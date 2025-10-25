using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class MessageRequest : DriverRequest
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("attachments")]
    public IList<Attachment> Attachments { get; set; } = [];

    [JsonPropertyName("chat")]
    public required Chat Chat { get; set; }
}