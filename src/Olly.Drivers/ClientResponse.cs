using Olly.Cards.Tasks;
using Olly.Storage.Models;

namespace Olly.Drivers;

public class ClientResponse
{
    public Message? Message { get; set; }
    public Message? Progress { get; set; }
    public TaskProgressCard TaskCard { get; } = new();
}