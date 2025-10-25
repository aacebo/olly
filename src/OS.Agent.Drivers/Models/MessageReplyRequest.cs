using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class MessageReplyRequest : MessageRequest
{
    [JsonPropertyName("reply_to")]
    public required Message ReplyTo { get; set; }
}