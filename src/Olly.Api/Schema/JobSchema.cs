using Olly.Services;
using Olly.Storage.Models.Jobs;

namespace Olly.Api.Schema;

[GraphQLName("Job")]
public class JobSchema(Job job) : ModelSchema
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = job.Id;

    [GraphQLName("type")]
    public string Type { get; set; } = job.Type;

    [GraphQLName("name")]
    public string Name { get; set; } = job.Name;

    [GraphQLName("title")]
    public string Title { get; set; } = job.Title;

    [GraphQLName("description")]
    public string? Description { get; set; } = job.Description;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = job.Entities.Select(entity => new EntitySchema(entity));

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = job.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; } = job.UpdatedAt;

    [GraphQLName("install")]
    public async Task<InstallSchema> GetInstall([Service] IInstallService installService, CancellationToken cancellationToken = default)
    {
        var install = await installService.GetById(job.InstallId, cancellationToken);

        if (install is null)
        {
            throw new InvalidDataException();
        }

        return new(install);
    }

    [GraphQLName("chat")]
    public async Task<ChatSchema?> GetChat([Service] IChatService chatService, CancellationToken cancellationToken = default)
    {
        if (job.ChatId is null) return null;
        var chat = await chatService.GetById(job.ChatId.Value, cancellationToken);
        return chat is null ? null : new(chat);
    }

    [GraphQLName("message")]
    public async Task<MessageSchema?> GetChat([Service] IMessageService messageService, CancellationToken cancellationToken = default)
    {
        if (job.MessageId is null) return null;
        var message = await messageService.GetById(job.MessageId.Value, cancellationToken);
        return message is null ? null : new(message);
    }

    [GraphQLName("parent")]
    public async Task<JobSchema?> GetParent([Service] IJobService jobService, CancellationToken cancellationToken = default)
    {
        if (job.ParentId is null) return null;
        var parent = await jobService.GetById(job.ParentId.Value, cancellationToken);
        return parent is null ? null : new(parent);
    }

    [GraphQLName("last_run")]
    public async Task<JobRunSchema?> GetLastRun([Service] IJobRunService runService, CancellationToken cancellationToken = default)
    {
        if (job.LastRunId is null) return null;
        var run = await runService.GetById(job.LastRunId.Value, cancellationToken);
        return run is null ? null : new(run);
    }

    [GraphQLName("runs")]
    public async Task<IEnumerable<JobRunSchema>> GetRuns([Service] IJobRunService runService, CancellationToken cancellationToken = default)
    {
        var runs = await runService.GetByJobId(job.Id, cancellationToken: cancellationToken);
        return runs.List.Select(run => new JobRunSchema(run));
    }

    [GraphQLName("approvals")]
    public async Task<IEnumerable<JobApprovalSchema>> GetApprovals([Service] IJobApprovalService approvalService, CancellationToken cancellationToken = default)
    {
        var approvals = await approvalService.GetByJobId(job.Id, cancellationToken: cancellationToken);
        return approvals.Select(approval => new JobApprovalSchema(approval));
    }
}