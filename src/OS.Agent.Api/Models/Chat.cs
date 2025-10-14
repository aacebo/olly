using System.Text.Json;
using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Models;

[Model]
public class Chat : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("tenant_id")]
    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [Column("parent_id")]
    [JsonPropertyName("parent_id")]
    public Guid? ParentId { get; set; }

    [Column("source_id")]
    [JsonPropertyName("source_id")]
    public required string SourceId { get; set; }

    [Column("source_type")]
    [JsonPropertyName("source_type")]
    public required SourceType SourceType { get; init; }

    [Column("type")]
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [Column("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [Column("data")]
    [JsonPropertyName("data")]
    public ChatData Data { get; set; } = new ChatData();

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
[JsonDerivedType(typeof(ChatData), typeDiscriminator: "chat")]
[JsonDerivedType(typeof(TeamsChatData), typeDiscriminator: "chat.teams")]
public class ChatData : Data
{
    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}

public class TeamsChatData : ChatData
{
    [JsonPropertyName("conversation")]
    public required Microsoft.Teams.Api.Conversation Conversation { get; set; }

    [JsonPropertyName("service_url")]
    public string? ServiceUrl { get; set; }

    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}