using System.Text.Json.Serialization;

using OS.Agent.Json;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[JsonDerivedFromType(typeof(Data), "chat.github.discussion")]
[JsonDerivedFromType(typeof(ChatData), "chat.github.discussion")]
public class GithubDiscussionData : ChatData
{
    [JsonPropertyName("discussion")]
    public required Octokit.Webhooks.Models.Discussion Discussion { get; set; }
}

public static partial class ChatDataExtensions
{
    public static GithubDiscussionData? GithubDiscussion(this ChatData data)
    {
        return data as GithubDiscussionData;
    }
}