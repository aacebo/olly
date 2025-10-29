using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

using SqlKata.Execution;

namespace OS.Agent.Services;

public interface IJobService
{
    Task<Job?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Job>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<Job> Create(Job value, CancellationToken cancellationToken = default);
    Task<Job> Update(Job value, CancellationToken cancellationToken = default);
}

public class JobService(IServiceProvider provider) : IJobService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<JobEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<JobEvent>>();
    private IJobStorage Storage { get; init; } = provider.GetRequiredService<IJobStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();

    public async Task<Job?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var job = Cache.Get<Job>(id);

        if (job is not null)
        {
            return job;
        }

        job = await Storage.GetById(id, cancellationToken);

        if (job is not null)
        {
            Cache.Set(job.Id, job);
        }

        return job;
    }

    public async Task<PaginationResult<Job>> GetByTenantId(Guid tenantId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByTenantId(tenantId, page, cancellationToken);
    }

    public async Task<IEnumerable<Job>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByParentId(parentId, cancellationToken);
    }

    public async Task<Job> Create(Job value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var job = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Job = job
        });

        return job;
    }

    public async Task<Job> Update(Job value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var job = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant,
            Job = job
        });

        return job;
    }
}