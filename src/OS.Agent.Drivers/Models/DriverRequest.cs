using System.Text.Json.Serialization;

namespace OS.Agent.Drivers.Models;

public abstract class DriverRequest
{
    [JsonIgnore]
    public required IServiceProvider Provider { get; set; }
}