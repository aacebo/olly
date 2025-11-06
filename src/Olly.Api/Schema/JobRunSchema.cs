using Olly.Services;
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
    public DateTimeOffset? StartedAt { get; set; } = run.StartedAt;

    [GraphQLName("ended_at")]
    public DateTimeOffset? EndedAt { get; set; } = run.EndedAt;

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = run.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; } = run.UpdatedAt;

    [GraphQLName("logs")]
    public async Task<IEnumerable<LogSchema>> GetLogs([Service] IServices services, CancellationToken cancellationToken = default)
    {
        var job = await services.Jobs.GetById(run.JobId, cancellationToken) ?? throw new Exception("job run not found");
        var install = await services.Installs.GetById(job.InstallId, cancellationToken) ?? throw new Exception("install not found");
        var account = await services.Accounts.GetById(install.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var res = await services.Logs.GetByTypeId(account.TenantId, Storage.Models.LogType.JobRun, run.Id.ToString(), cancellationToken: cancellationToken);
        return res.List.Select(log => new LogSchema(log));
    }
}