using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Models;

public abstract class DriverRequest
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; set; }

    [JsonPropertyName("user")]
    public required User User { get; set; }

    [JsonPropertyName("account")]
    public required Account Account { get; set; }

    [JsonPropertyName("install")]
    public required Install Install { get; set; }

    [JsonIgnore]
    public required IServiceProvider Provider { get; set; }
}