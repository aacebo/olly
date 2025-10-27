using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Services;

public interface IAccountService
{
    Task<Account?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByUserId(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByTenantUserId(Guid tenantId, Guid userId, SourceType? type = null, CancellationToken cancellationToken = default);
    Task<Account> Create(Account value, CancellationToken cancellationToken = default);
    Task<Account> Update(Account value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class AccountService(IServiceProvider provider) : IAccountService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<AccountEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<AccountEvent>>();
    private IAccountStorage Storage { get; init; } = provider.GetRequiredService<IAccountStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();
    private IUserService Users { get; init; } = provider.GetRequiredService<IUserService>();

    public async Task<Account?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var account = Cache.Get<Account>(id);

        if (account is not null)
        {
            return account;
        }

        account = await Storage.GetById(id, cancellationToken);

        if (account is not null)
        {
            Cache.Set(account.Id, account);
        }

        return account;
    }

    public async Task<Account?> GetBySourceId(Guid tenantId, SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        var account = await Storage.GetBySourceId(tenantId, type, sourceId, cancellationToken);

        if (account is not null)
        {
            Cache.Set(account.Id, account);
        }

        return account;
    }

    public async Task<IEnumerable<Account>> GetByUserId(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByUserId(userId, cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByTenantId(tenantId, cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetByTenantUserId(Guid tenantId, Guid userId, SourceType? type = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByTenantUserId(tenantId, userId, type, cancellationToken);
    }

    public async Task<Account> Create(Account value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = value.UserId is not null ? await Users.GetById(value.UserId.Value, cancellationToken) : null;
        var account = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Account = account,
            CreatedBy = user
        });

        if (tenant.Name is null && value.Name != tenant.Name)
        {
            tenant.Name = account.Name;
            await Tenants.Update(tenant, cancellationToken);
        }

        return account;
    }

    public async Task<Account> Update(Account value, CancellationToken cancellationToken = default)
    {
        var tenant = await Tenants.GetById(value.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = value.UserId is not null ? await Users.GetById(value.UserId.Value, cancellationToken) : null;
        var account = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant,
            Account = account,
            CreatedBy = user
        });

        if (tenant.Name is null && value.Name != tenant.Name)
        {
            tenant.Name = account.Name;
            await Tenants.Update(tenant, cancellationToken);
        }

        return account;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await GetById(id, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = account.UserId is not null ? await Users.GetById(account.UserId.Value, cancellationToken) : null;

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Tenant = tenant,
            Account = account,
            CreatedBy = user
        });
    }
}