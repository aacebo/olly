using System.Text.Json.Serialization;

using SqlKata;

namespace Olly.Storage.Models.Jobs;

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

    [Column("chat_id")]
    [JsonPropertyName("chat_id")]
    public Guid? ChatId { get; init; }

    [Column("message_id")]
    [JsonPropertyName("message_id")]
    public Guid? MessageId { get; init; }

    [Column("last_run_id")]
    [JsonPropertyName("last_run_id")]
    public Guid? LastRunId { get; set; }

    [Column("type")]
    [JsonPropertyName("type")]
    public JobType Type { get; set; } = JobType.Async;

    [Column("name")]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [Column("title")]
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [Column("description")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [Column("entities")]
    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

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
}

[JsonConverter(typeof(Converter<JobType>))]
public class JobType(string value) : StringEnum(value)
{
    public static readonly JobType Sync = new("sync");
    public bool IsSync => Sync.Equals(Value);

    public static readonly JobType Async = new("async");
    public bool IsAsync => Async.Equals(Value);
}