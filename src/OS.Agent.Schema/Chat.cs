using System.Text.Json.Serialization;

namespace OS.Agent.Schema;

public class Chat
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("tenant")]
    public required TenantPartial Tenant { get; init; }

    [JsonPropertyName("parent")]
    public ChatPartial? Parent { get; set; }

    [JsonPropertyName("source")]
    public required Source Source { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; set; }
}

public class ChatPartial
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}