using System.Text.Json.Serialization;

namespace OS.Agent.Drivers.Github.Models;

public class GithubDiscussionComment
{
    [JsonPropertyName("id")]
    public required Octokit.GraphQL.ID Id { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("upvotes")]
    public required int UpVotes { get; set; }

    [JsonPropertyName("body")]
    public required string Body { get; set; }
}