using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class MessageUpdateRequest : MessageRequest
{
    [JsonPropertyName("message")]
    public required Message Message { get; set; }
}