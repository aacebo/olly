using System.Text.Json.Serialization;

using Olly.Storage.Models;
using Olly.Storage.Models.Jobs;

namespace Olly.Events;

public class JobApprovalEvent(ActionType action) : Event(EntityType.Approval, action)
{
    [JsonPropertyName("tenant")]
    public required Tenant Tenant { get; init; }

    [JsonPropertyName("job")]
    public required Job Job { get; init; }

    [JsonPropertyName("account")]
    public required Account Account { get; init; }

    [JsonPropertyName("approval")]
    public required Approval Approval { get; init; }
}