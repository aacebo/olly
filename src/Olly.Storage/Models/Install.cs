using System.Text.Json.Serialization;

using SqlKata;

namespace Olly.Storage.Models;

[Model]
public class Install : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("user_id")]
    [JsonPropertyName("user_id")]
    public required Guid UserId { get; init; }

    [Column("account_id")]
    [JsonPropertyName("account_id")]
    public required Guid AccountId { get; init; }

    [Column("chat_id")]
    [JsonPropertyName("chat_id")]
    public required Guid ChatId { get; init; }

    [Column("message_id")]
    [JsonPropertyName("message_id")]
    public Guid? MessageId { get; init; }

    [Column("source_id")]
    [JsonPropertyName("source_id")]
    public required string SourceId { get; set; }

    [Column("source_type")]
    [JsonPropertyName("source_type")]
    public required SourceType SourceType { get; init; }

    [Column("status")]
    [JsonPropertyName("status")]
    public InstallStatus Status { get; set; } = InstallStatus.InProgress;

    [Column("url")]
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [Column("access_token")]
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [Column("expires_at")]
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [Column("entities")]
    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Install Copy()
    {
        return (Install)MemberwiseClone();
    }
}

[JsonConverter(typeof(Converter<InstallStatus>))]
public class InstallStatus(string value) : StringEnum(value)
{
    public static readonly InstallStatus InProgress = new("in-progress");
    public bool IsInProgress => InProgress.Equals(Value);

    public static readonly InstallStatus Success = new("success");
    public bool IsSuccess => Success.Equals(Value);

    public static readonly InstallStatus Error = new("error");
    public bool IsError => Error.Equals(Value);
}