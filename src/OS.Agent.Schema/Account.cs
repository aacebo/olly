using System.Text.Json.Serialization;

namespace OS.Agent.Schema;

public class Account
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("user")]
    public UserPartial? User { get; set; }

    [JsonPropertyName("tenant")]
    public required TenantPartial Tenant { get; init; }

    [JsonPropertyName("source")]
    public required Source Source { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; set; }
}

public class AccountPartial
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("source")]
    public required Source Source { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}