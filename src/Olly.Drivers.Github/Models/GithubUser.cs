using System.Text.Json.Serialization;

namespace Olly.Drivers.Github.Models;

public class GithubUser
{
    [JsonPropertyName("id")]
    public required long Id { get; set; }

    [JsonPropertyName("node_id")]
    public required string NodeId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("login")]
    public required string Login { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("avatar_url")]
    public required string AvatarUrl { get; set; }
}