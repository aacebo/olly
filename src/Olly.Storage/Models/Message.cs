using System.Text.Json.Serialization;

using SqlKata;

namespace Olly.Storage.Models;

[Model]
public class Message : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("chat_id")]
    [JsonPropertyName("chat_id")]
    public required Guid ChatId { get; init; }

    [Column("account_id")]
    [JsonPropertyName("account_id")]
    public Guid? AccountId { get; init; }

    [Column("reply_to_id")]
    [JsonPropertyName("reply_to_id")]
    public Guid? ReplyToId { get; set; }

    [Column("source_id")]
    [JsonPropertyName("source_id")]
    public required string SourceId { get; set; }

    [Column("source_type")]
    [JsonPropertyName("source_type")]
    public required SourceType SourceType { get; init; }

    [Column("url")]
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [Column("text")]
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [Column("attachments")]
    [JsonPropertyName("attachments")]
    public List<Attachment> Attachments { get; set; } = [];

    [Column("entities")]
    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Message Copy()
    {
        return (Message)MemberwiseClone();
    }
}