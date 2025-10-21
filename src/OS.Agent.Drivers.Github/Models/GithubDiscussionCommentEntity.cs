using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github.Models;

[Entity("github.discussion.comment")]
public class GithubDiscussionCommentEntity() : Entity("github.discussion.comment")
{
    [JsonPropertyName("comment")]
    public required GithubDiscussionComment Comment { get; set; }
}