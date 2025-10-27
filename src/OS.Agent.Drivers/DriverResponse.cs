using OS.Agent.Cards.Tasks;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers;

public class DriverResponse
{
    public Message? Message { get; set; }
    public Message? Progress { get; set; }
    public TaskProgressCard TaskCard { get; } = new();
}