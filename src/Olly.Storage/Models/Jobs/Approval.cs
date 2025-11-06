using System.Text.Json.Serialization;

using SqlKata;

namespace Olly.Storage.Models.Jobs;

[Model]
public class Approval : Model
{
    [Column("job_id")]
    [JsonPropertyName("job_id")]
    public required Guid JobId { get; init; }

    [Column("account_id")]
    [JsonPropertyName("account_id")]
    public required Guid AccountId { get; init; }

    [Column("status")]
    [JsonPropertyName("status")]
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    [Column("required")]
    [JsonPropertyName("required")]
    public bool Required { get; set; } = false;

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Approval Copy()
    {
        return (Approval)MemberwiseClone();
    }
}

[JsonConverter(typeof(Converter<ApprovalStatus>))]
public class ApprovalStatus(string value) : StringEnum(value)
{
    public static readonly ApprovalStatus Pending = new("pending");
    public bool IsPending => Pending.Equals(Value);

    public static readonly ApprovalStatus Approved = new("approved");
    public bool IsApproved => Approved.Equals(Value);

    public static readonly ApprovalStatus Rejected = new("rejected");
    public bool IsRejected => Rejected.Equals(Value);
}