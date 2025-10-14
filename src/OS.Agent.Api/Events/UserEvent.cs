using System.Text.Json.Serialization;

using OS.Agent.Models;

namespace OS.Agent.Events;

public class UserEvent
{
    [JsonPropertyName("user")]
    public required User User { get; init; }
}