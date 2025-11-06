using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using Olly.Events;
using Olly.Storage;
using Olly.Storage.Models.Jobs;

namespace Olly.Services;

public interface IJobApprovalService
{
    Task<Approval?> GetOne(Guid jobId, Guid accountId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Approval>> GetByJobId(Guid jobId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Approval>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default);
    Task<Approval> Create(Approval value, CancellationToken cancellationToken = default);
    Task<Approval> Update(Approval value, CancellationToken cancellationToken = default);
    Task Delete(Guid jobId, Guid accountId, CancellationToken cancellationToken = default);
}

public class JobApprovalService(IServiceProvider provider) : IJobApprovalService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<JobApprovalEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<JobApprovalEvent>>();
    private IJobApprovalStorage Storage { get; init; } = provider.GetRequiredService<IJobApprovalStorage>();
    private IJobService Jobs { get; init; } = provider.GetRequiredService<IJobService>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();
    private IAccountService Accounts { get; init; } = provider.GetRequiredService<IAccountService>();

    public async Task<Approval?> GetOne(Guid jobId, Guid accountId, CancellationToken cancellationToken = default)
    {
        var id = $"{jobId}-{accountId}";
        var approval = Cache.Get<Approval>(id);

        if (approval is not null)
        {
            return approval;
        }

        approval = await Storage.GetOne(jobId, accountId, cancellationToken);

        if (approval is not null)
        {
            Cache.Set(id, approval);
        }

        return approval;
    }

    public async Task<IEnumerable<Approval>> GetByJobId(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByJobId(jobId, cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByAccountId(accountId, cancellationToken);
    }

    public async Task<Approval> Create(Approval value, CancellationToken cancellationToken = default)
    {
        var job = await Jobs.GetById(value.JobId, cancellationToken) ?? throw new Exception("job not found");
        var account = await Accounts.GetById(value.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var approval = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Job = job,
            Account = account,
            Approval = approval
        });

        return approval;
    }

    public async Task<Approval> Update(Approval value, CancellationToken cancellationToken = default)
    {
        var job = await Jobs.GetById(value.JobId, cancellationToken) ?? throw new Exception("job not found");
        var account = await Accounts.GetById(value.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var approval = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant,
            Job = job,
            Account = account,
            Approval = approval
        });

        return approval;
    }

    public async Task Delete(Guid jobId, Guid accountId, CancellationToken cancellationToken = default)
    {
        var approval = await GetOne(jobId, accountId, cancellationToken) ?? throw new Exception("approval not found");
        var account = await Accounts.GetById(approval.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var job = await Jobs.GetById(approval.JobId, cancellationToken) ?? throw new Exception("job not found");

        await Storage.Delete(jobId, accountId, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Tenant = tenant,
            Job = job,
            Account = account,
            Approval = approval
        });
    }
}