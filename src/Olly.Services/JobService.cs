using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using Olly.Events;
using Olly.Storage;
using Olly.Storage.Models;

using SqlKata.Execution;

namespace Olly.Services;

public interface IJobService
{
    Task<Job?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Job>> GetByInstallId(Guid installId, Page? page = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default);
    Task<PaginationResult<Job>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> GetBlocking(Guid chatId, CancellationToken cancellationToken = default);

    Task<Job> Create(Job value, CancellationToken cancellationToken = default);
    Task<Job> Update(Job value, CancellationToken cancellationToken = default);

    Task AddChat(Guid chatId, Guid jobId, bool async = false, CancellationToken cancellationToken = default);
    Task DelChat(Guid chatId, Guid jobId, CancellationToken cancellationToken = default);
}

public class JobService(IServiceProvider provider) : IJobService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<JobEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<JobEvent>>();
    private IJobStorage Storage { get; init; } = provider.GetRequiredService<IJobStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();
    private IAccountService Accounts { get; init; } = provider.GetRequiredService<IAccountService>();
    private IUserService Users { get; init; } = provider.GetRequiredService<IUserService>();
    private IInstallService Installs { get; init; } = provider.GetRequiredService<IInstallService>();
    private IChatService Chats { get; init; } = provider.GetRequiredService<IChatService>();

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

    public async Task<PaginationResult<Job>> GetByInstallId(Guid installId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByInstallId(installId, page, cancellationToken);
    }

    public async Task<IEnumerable<Job>> GetByParentId(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByParentId(parentId, cancellationToken);
    }

    public async Task<PaginationResult<Job>> GetByChatId(Guid chatId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByChatId(chatId, page, cancellationToken);
    }

    public async Task<IEnumerable<Job>> GetBlocking(Guid chatId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetBlocking(chatId, cancellationToken);
    }

    public async Task<Job> Create(Job value, CancellationToken cancellationToken = default)
    {
        var install = await Installs.GetById(value.InstallId, cancellationToken) ?? throw new Exception("install not found");
        var account = await Accounts.GetById(install.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = await Users.GetById(install.UserId, cancellationToken) ?? throw new Exception("user not found");
        var job = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            User = user,
            Job = job
        });

        return job;
    }

    public async Task<Job> Update(Job value, CancellationToken cancellationToken = default)
    {
        var install = await Installs.GetById(value.InstallId, cancellationToken) ?? throw new Exception("install not found");
        var account = await Accounts.GetById(install.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = await Users.GetById(install.UserId, cancellationToken) ?? throw new Exception("user not found");
        var job = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            User = user,
            Job = job
        });

        return job;
    }

    public async Task AddChat(Guid chatId, Guid jobId, bool async = false, CancellationToken cancellationToken = default)
    {
        await Storage.AddChat(chatId, jobId, async, cancellationToken: cancellationToken);
    }

    public async Task DelChat(Guid chatId, Guid jobId, CancellationToken cancellationToken = default)
    {
        await Storage.DelChat(chatId, jobId, cancellationToken: cancellationToken);
    }
}