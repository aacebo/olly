using System.Text.Json.Serialization;

namespace OS.Agent.Schema;

public class Tenant
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("sources")]
    public List<Source> Sources { get; set; } = [];

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("entities")]
    public Entities Entities { get; set; } = [];

    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; set; }
}

public class TenantPartial
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}