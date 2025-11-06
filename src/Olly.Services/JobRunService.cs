using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using Olly.Events;
using Olly.Storage;
using Olly.Storage.Models.Jobs;

using SqlKata.Execution;

namespace Olly.Services;

public interface IJobRunService
{
    Task<Run?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<PaginationResult<Run>> GetByJobId(Guid jobId, Page? page = null, CancellationToken cancellationToken = default);
    Task<Run> Create(Run value, CancellationToken cancellationToken = default);
    Task<Run> Update(Run value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class JobRunService(IServiceProvider provider) : IJobRunService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<JobRunEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<JobRunEvent>>();
    private IJobRunStorage Storage { get; init; } = provider.GetRequiredService<IJobRunStorage>();
    private IJobService Jobs { get; init; } = provider.GetRequiredService<IJobService>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();
    private IInstallService Installs { get; init; } = provider.GetRequiredService<IInstallService>();
    private IAccountService Accounts { get; init; } = provider.GetRequiredService<IAccountService>();
    private IUserService Users { get; init; } = provider.GetRequiredService<IUserService>();
    private IChatService Chats { get; init; } = provider.GetRequiredService<IChatService>();
    private IMessageService Messages { get; init; } = provider.GetRequiredService<IMessageService>();

    public async Task<Run?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var run = Cache.Get<Run>(id);

        if (run is not null)
        {
            return run;
        }

        run = await Storage.GetById(id, cancellationToken);

        if (run is not null)
        {
            Cache.Set(run.Id, run);
        }

        return run;
    }

    public async Task<PaginationResult<Run>> GetByJobId(Guid jobId, Page? page = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByJobId(jobId, page, cancellationToken);
    }

    public async Task<Run> Create(Run value, CancellationToken cancellationToken = default)
    {
        var job = await Jobs.GetById(value.JobId, cancellationToken) ?? throw new Exception("job not found");
        var install = await Installs.GetById(job.InstallId, cancellationToken) ?? throw new Exception("install not found");
        var account = await Accounts.GetById(install.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = await Users.GetById(install.UserId, cancellationToken) ?? throw new Exception("user not found");
        var chat = job.ChatId is not null ? await Chats.GetById(job.ChatId.Value, cancellationToken) : null;
        var message = job.MessageId is not null ? await Messages.GetById(job.MessageId.Value, cancellationToken) : null;
        var run = await Storage.Create(value, cancellationToken: cancellationToken);

        job.LastRunId = run.Id;
        job = await Jobs.Update(job, cancellationToken);
        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            User = user,
            Chat = chat,
            Message = message,
            Job = job,
            Run = run
        });

        return run;
    }

    public async Task<Run> Update(Run value, CancellationToken cancellationToken = default)
    {
        var job = await Jobs.GetById(value.JobId, cancellationToken) ?? throw new Exception("job not found");
        var install = await Installs.GetById(job.InstallId, cancellationToken) ?? throw new Exception("install not found");
        var account = await Accounts.GetById(install.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = await Users.GetById(install.UserId, cancellationToken) ?? throw new Exception("user not found");
        var chat = job.ChatId is not null ? await Chats.GetById(job.ChatId.Value, cancellationToken) : null;
        var message = job.MessageId is not null ? await Messages.GetById(job.MessageId.Value, cancellationToken) : null;
        var run = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            User = user,
            Chat = chat,
            Message = message,
            Job = job,
            Run = run
        });

        return run;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var run = await GetById(id, cancellationToken) ?? throw new Exception("run not found");
        var job = await Jobs.GetById(run.JobId, cancellationToken) ?? throw new Exception("job not found");
        var install = await Installs.GetById(job.InstallId, cancellationToken) ?? throw new Exception("install not found");
        var account = await Accounts.GetById(install.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = await Users.GetById(install.UserId, cancellationToken) ?? throw new Exception("user not found");
        var chat = job.ChatId is not null ? await Chats.GetById(job.ChatId.Value, cancellationToken) : null;
        var message = job.MessageId is not null ? await Messages.GetById(job.MessageId.Value, cancellationToken) : null;

        if (job.LastRunId == run.Id)
        {
            job.LastRunId = null;
            job = await Jobs.Update(job, cancellationToken);
        }

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            User = user,
            Chat = chat,
            Message = message,
            Job = job,
            Run = run
        });
    }
}