using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class InstallRequest : DriverRequest
{
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    [JsonPropertyName("message")]
    public Message? Message { get; set; }
}