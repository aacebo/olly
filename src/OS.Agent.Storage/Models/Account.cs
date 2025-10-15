using System.Text.Json;
using System.Text.Json.Serialization;

using OS.Agent.Drivers.Github;

using SqlKata;

namespace OS.Agent.Storage.Models;

[Model]
public class Account : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("user_id")]
    [JsonPropertyName("user_id")]
    public Guid? UserId { get; set; }

    [Column("tenant_id")]
    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [Column("source_id")]
    [JsonPropertyName("source_id")]
    public required string SourceId { get; set; }

    [Column("source_type")]
    [JsonPropertyName("source_type")]
    public required SourceType SourceType { get; init; }

    [Column("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [Column("data")]
    [JsonPropertyName("data")]
    public AccountData Data { get; set; } = new AccountData();

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[JsonPolymorphic]
[JsonDerivedType(typeof(AccountData), typeDiscriminator: "account")]
[JsonDerivedType(typeof(GithubAccountData), typeDiscriminator: "account.github")]
[JsonDerivedType(typeof(TeamsAccountData), typeDiscriminator: "account.teams")]
public class AccountData : Data
{
    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}

public class GithubAccountData : AccountData
{
    [JsonConverter(typeof(OctokitJsonConverter<Octokit.User>))]
    [JsonPropertyName("user")]
    public required Octokit.User User { get; set; }

    [JsonConverter(typeof(OctokitJsonConverter<Octokit.Installation>))]
    [JsonPropertyName("install")]
    public required Octokit.Installation Install { get; set; }

    [JsonConverter(typeof(OctokitJsonConverter<Octokit.AccessToken>))]
    [JsonPropertyName("access_token")]
    public required Octokit.AccessToken AccessToken { get; set; }

    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}

public class TeamsAccountData : AccountData
{
    [JsonPropertyName("user")]
    public required Microsoft.Teams.Api.Account User { get; set; }

    [JsonExtensionData]
    public new IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();
}