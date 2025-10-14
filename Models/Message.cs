using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Activities;

using SqlKata;

namespace OS.Agent.Models;

[Model]
public class Message : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("chat_id")]
    [JsonPropertyName("chat_id")]
    public required Guid ChatId { get; init; }

    [AllowNull]
    [Column("account_id")]
    [JsonPropertyName("account_id")]
    public Guid? AccountId { get; init; }

    [Column("source_id")]
    [JsonPropertyName("source_id")]
    public required string SourceId { get; set; }

    [Column("source_type")]
    [JsonPropertyName("source_type")]
    public required SourceType SourceType { get; init; }

    [Column("text")]
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [Column("data")]
    [JsonPropertyName("data")]
    public MessageData Data { get; set; } = new MessageData();

    [Column("notes")]
    [JsonPropertyName("notes")]
    public List<Note> Notes { get; set; } = [];

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[JsonPolymorphic]
[JsonDerivedType(typeof(MessageData), typeDiscriminator: "message")]
[JsonDerivedType(typeof(TeamsMessageData), typeDiscriminator: "message.teams")]
public class MessageData : Data
{
    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}

public class TeamsMessageData : MessageData
{
    [JsonPropertyName("activity")]
    public required MessageActivity Activity { get; set; }

    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}