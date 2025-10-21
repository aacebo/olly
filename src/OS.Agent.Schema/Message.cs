using System.Text.Json.Serialization;

namespace OS.Agent.Schema;

public class Message
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("chat")]
    public required ChatPartial Chat { get; init; }

    [JsonPropertyName("account")]
    public AccountPartial? Account { get; init; }

    [JsonPropertyName("reply_to")]
    public MessagePartial? ReplyTo { get; set; }

    [JsonPropertyName("source")]
    public required Source Source { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class MessagePartial
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }
}