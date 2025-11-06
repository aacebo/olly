using Olly.Services;
using Olly.Storage.Models.Jobs;

namespace Olly.Api.Schema;

[GraphQLName("JobApproval")]
public class JobApprovalSchema(Approval approval) : ModelSchema
{
    [GraphQLName("status")]
    public string Status { get; set; } = approval.Status;

    [GraphQLName("required")]
    public bool Required { get; set; } = approval.Required;

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = approval.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; } = approval.UpdatedAt;

    [GraphQLName("job")]
    public async Task<JobSchema> GetJob([Service] IJobService jobService, CancellationToken cancellationToken = default)
    {
        var job = await jobService.GetById(approval.JobId, cancellationToken);

        if (job is null)
        {
            throw new InvalidDataException();
        }

        return new(job);
    }

    [GraphQLName("account")]
    public async Task<AccountSchema> GetAccount([Service] IAccountService accountService, CancellationToken cancellationToken = default)
    {
        var account = await accountService.GetById(approval.AccountId, cancellationToken);

        if (account is null)
        {
            throw new InvalidDataException();
        }

        return new(account);
    }
}