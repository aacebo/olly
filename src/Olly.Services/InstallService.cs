using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using Olly.Events;
using Olly.Storage;
using Olly.Storage.Models;

using SqlKata.Execution;

namespace Olly.Services;

public interface IInstallService
{
    Task<Install?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Install>> GetByUserId(Guid userId, Query? query = null, CancellationToken cancellationToken = default);
    Task<Install?> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default);
    Task<Install?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default);
    Task<Install> Create(Install value, CancellationToken cancellationToken = default);
    Task<Install> Create(Install value, Chat chat, CancellationToken cancellationToken = default);
    Task<Install> Update(Install value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class InstallService(IServiceProvider provider) : IInstallService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<InstallEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<InstallEvent>>();
    private IInstallStorage Storage { get; init; } = provider.GetRequiredService<IInstallStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();
    private IAccountService Accounts { get; init; } = provider.GetRequiredService<IAccountService>();
    private IChatService Chats { get; init; } = provider.GetRequiredService<IChatService>();
    private IMessageStorage Messages { get; init; } = provider.GetRequiredService<IMessageStorage>();
    private IUserService Users { get; init; } = provider.GetRequiredService<IUserService>();

    public async Task<Install?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var install = Cache.Get<Install>(id);

        if (install is not null)
        {
            return install;
        }

        install = await Storage.GetById(id, cancellationToken);

        if (install is not null)
        {
            Cache.Set(install.Id, install);
        }

        return install;
    }

    public async Task<IEnumerable<Install>> GetByUserId(Guid userId, Query? query = null, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByUserId(userId, query, cancellationToken);
    }

    public async Task<Install?> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await Storage.GetByAccountId(accountId, cancellationToken);
    }

    public async Task<Install?> GetBySourceId(SourceType type, string sourceId, CancellationToken cancellationToken = default)
    {
        var install = await Storage.GetBySourceId(type, sourceId, cancellationToken);

        if (install is not null)
        {
            Cache.Set(install.Id, install);
        }

        return install;
    }

    public async Task<Install> Create(Install value, CancellationToken cancellationToken = default)
    {
        var account = await Accounts.GetById(value.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = await Users.GetById(value.UserId, cancellationToken) ?? throw new Exception("user not found");
        var message = value.MessageId is not null ? await Messages.GetById(value.MessageId.Value, cancellationToken) : null;
        var chat = message is not null ? await Chats.GetById(message.ChatId, cancellationToken) : null;
        var install = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            Chat = chat,
            Message = message,
            CreatedBy = user
        });

        return install;
    }

    public async Task<Install> Create(Install value, Chat chat, CancellationToken cancellationToken = default)
    {
        var account = await Accounts.GetById(value.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = await Users.GetById(value.UserId, cancellationToken) ?? throw new Exception("user not found");
        var message = value.MessageId is not null ? await Messages.GetById(value.MessageId.Value, cancellationToken) : null;
        var install = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            Chat = chat,
            Message = message,
            CreatedBy = user
        });

        return install;
    }

    public async Task<Install> Update(Install value, CancellationToken cancellationToken = default)
    {
        var account = await Accounts.GetById(value.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = await Users.GetById(value.UserId, cancellationToken) ?? throw new Exception("user not found");
        var message = value.MessageId is not null ? await Messages.GetById(value.MessageId.Value, cancellationToken) : null;
        var chat = message is not null ? await Chats.GetById(message.ChatId, cancellationToken) : null;
        var install = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            Chat = chat,
            Message = message,
            CreatedBy = user
        });

        return install;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var install = await GetById(id, cancellationToken) ?? throw new Exception("install not found");
        var account = await Accounts.GetById(install.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var user = await Users.GetById(install.UserId, cancellationToken) ?? throw new Exception("user not found");
        var message = install.MessageId is not null ? await Messages.GetById(install.MessageId.Value, cancellationToken) : null;
        var chat = message is not null ? await Chats.GetById(message.ChatId, cancellationToken) : null;

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Tenant = tenant,
            Account = account,
            Install = install,
            Chat = chat,
            Message = message,
            CreatedBy = user
        });
    }
}