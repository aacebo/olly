using System.Text.Json.Serialization;

using SqlKata;

namespace Olly.Storage.Models;

[Model]
public class Job : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("install_id")]
    [JsonPropertyName("install_id")]
    public required Guid InstallId { get; init; }

    [Column("parent_id")]
    [JsonPropertyName("parent_id")]
    public Guid? ParentId { get; init; }

    [Column("name")]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [Column("status")]
    [JsonPropertyName("status")]
    public JobStatus Status { get; set; } = JobStatus.Pending;

    [Column("message")]
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [Column("entities")]
    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

    [Column("started_at")]
    [JsonPropertyName("started_at")]
    public DateTimeOffset? StartedAt { get; set; }

    [Column("ended_at")]
    [JsonPropertyName("ended_at")]
    public DateTimeOffset? EndedAt { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Job Copy()
    {
        return (Job)MemberwiseClone();
    }

    public Job Start()
    {
        Status = JobStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
        EndedAt = null;
        return this;
    }

    public Job Success()
    {
        Status = JobStatus.Success;
        Message = null;
        EndedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public Job Error(string message)
    {
        Status = JobStatus.Error;
        Message = message;
        EndedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public Job Error(Exception ex)
    {
        Status = JobStatus.Error;
        Message = ex.ToString();
        EndedAt = DateTimeOffset.UtcNow;
        return this;
    }
}

[JsonConverter(typeof(Converter<JobStatus>))]
public class JobStatus(string value) : StringEnum(value)
{
    public static readonly JobStatus Pending = new("pending");
    public bool IsPending => Pending.Equals(Value);

    public static readonly JobStatus Running = new("running");
    public bool IsRunning => Running.Equals(Value);

    public static readonly JobStatus Success = new("success");
    public bool IsSuccess => Success.Equals(Value);

    public static readonly JobStatus Error = new("error");
    public bool IsError => Error.Equals(Value);
}