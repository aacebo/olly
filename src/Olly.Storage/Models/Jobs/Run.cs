using System.Text.Json.Serialization;

using SqlKata;

namespace Olly.Storage.Models.Jobs;

[Model]
public class Run : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("job_id")]
    [JsonPropertyName("job_id")]
    public required Guid JobId { get; init; }

    [Column("status")]
    [JsonPropertyName("status")]
    public JobStatus Status { get; set; } = JobStatus.Running;

    [Column("status_message")]
    [JsonPropertyName("status_message")]
    public string? StatusMessage { get; set; }

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

    public Run Copy()
    {
        return (Run)MemberwiseClone();
    }

    public Run Start()
    {
        Status = JobStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
        EndedAt = null;
        return this;
    }

    public Run Success()
    {
        Status = JobStatus.Success;
        StatusMessage = null;
        EndedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public Run Error(string message)
    {
        Status = JobStatus.Error;
        StatusMessage = message;
        EndedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public Run Error(Exception ex)
    {
        Status = JobStatus.Error;
        StatusMessage = ex.ToString();
        EndedAt = DateTimeOffset.UtcNow;
        return this;
    }
}

[JsonConverter(typeof(Converter<JobStatus>))]
public class JobStatus(string value) : StringEnum(value)
{
    public static readonly JobStatus Running = new("running");
    public bool IsRunning => Running.Equals(Value);

    public static readonly JobStatus Success = new("success");
    public bool IsSuccess => Success.Equals(Value);

    public static readonly JobStatus Warning = new("warning");
    public bool IsWarning => Warning.Equals(Value);

    public static readonly JobStatus Error = new("error");
    public bool IsError => Error.Equals(Value);
}