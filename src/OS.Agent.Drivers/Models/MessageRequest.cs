using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class MessageRequest
{
    [JsonPropertyName("reply_to_id")]
    public string? ReplyToId { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("attachments")]
    public IList<Attachment> Attachments { get; set; } = [];

    [JsonPropertyName("from")]
    public required Account From { get; set; }

    [JsonPropertyName("chat")]
    public required Chat Chat { get; set; }
}