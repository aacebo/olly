using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public class UnInstallRequest
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; set; }

    [JsonPropertyName("account")]
    public required Account Account { get; set; }
}