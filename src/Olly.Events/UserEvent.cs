using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Events;

public class UserEvent(ActionType action) : Event(EntityType.User, action)
{
    [JsonPropertyName("user")]
    public required User User { get; init; }
}