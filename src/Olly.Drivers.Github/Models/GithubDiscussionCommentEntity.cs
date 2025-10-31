using System.Text.Json.Serialization;

using Olly.Storage.Models;

namespace Olly.Drivers.Github.Models;

[Entity("github.discussion.comment")]
public class GithubDiscussionCommentEntity() : Entity("github.discussion.comment")
{
    [JsonPropertyName("comment")]
    public required GithubDiscussionComment Comment { get; set; }
}