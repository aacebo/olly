using Olly.Storage.Models.Jobs;

namespace Olly.Api.Schema;

[GraphQLName("JobRun")]
public class JobRunSchema(Run run) : ModelSchema
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = run.Id;

    [GraphQLName("status")]
    public string Status { get; set; } = run.Status;

    [GraphQLName("status_message")]
    public string? StatusMessage { get; set; } = run.StatusMessage;

    [GraphQLName("started_at")]
    public DateTimeOffset? StartedAt { get; set; }

    [GraphQLName("ended_at")]
    public DateTimeOffset? EndedAt { get; set; }

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = run.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; } = run.UpdatedAt;
}