using OS.Agent.Cards.Tasks;
using OS.Agent.Storage.Models;

namespace OS.Agent.Contexts;

/// <summary>
/// an agents response that represents several status/progress
/// updates and a final message output
/// </summary>
public class AgentResponse
{
    public Message? Message { get; set; }
    public Message? Progress { get; set; }
    public TaskProgressCard TaskCard { get; } = new();
}