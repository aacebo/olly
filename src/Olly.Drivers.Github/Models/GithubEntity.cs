using System.Text.Json.Serialization;

using Olly.Drivers.Github.Json;
using Olly.Drivers.Github.Settings;
using Olly.Storage.Models;

namespace Olly.Drivers.Github.Models;

[Entity("github")]
[Entity("github.repository")]
[Entity("github.issue")]
[Entity("github.issue.comment")]
public class GithubEntity : Entity
{
    [JsonPropertyName("repository")]
    [JsonConverter(typeof(GithubJsonConverter<Octokit.Repository>))]
    public Octokit.Repository? Repository { get; set; }

    [JsonPropertyName("settings")]
    public GithubRepositorySettings? Settings { get; set; }

    [JsonPropertyName("issue")]
    [JsonConverter(typeof(GithubJsonConverter<Octokit.IssueUpdate>))]
    public Octokit.IssueUpdate? Issue { get; set; }

    [JsonPropertyName("issue_comment")]
    [JsonConverter(typeof(GithubJsonConverter<Octokit.IssueComment>))]
    public Octokit.IssueComment? IssueComment { get; set; }

    public GithubEntity() : base("github")
    {

    }

    public GithubEntity(Octokit.Repository repository) : base("github.repository")
    {
        Repository = repository;
    }

    public GithubEntity(Octokit.IssueUpdate issue) : base("github.issue")
    {
        Issue = issue;
    }

    public GithubEntity(Octokit.IssueComment issueComment) : base("github.issue.comment")
    {
        IssueComment = issueComment;
    }
}