using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Services;

public interface ITokenService
{
    Task<Token?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Token?> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default);
    Task<Token> Create(Token value, CancellationToken cancellationToken = default);
    Task<Token> Update(Token value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class TokenService(IServiceProvider provider) : ITokenService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<TokenEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<TokenEvent>>();
    private ITokenStorage Storage { get; init; } = provider.GetRequiredService<ITokenStorage>();
    private ITenantService Tenants { get; init; } = provider.GetRequiredService<ITenantService>();
    private IAccountService Accounts { get; init; } = provider.GetRequiredService<IAccountService>();

    public async Task<Token?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var token = Cache.Get<Token>(id);

        if (token is not null)
        {
            return token;
        }

        token = await Storage.GetById(id, cancellationToken);

        if (token is not null)
        {
            Cache.Set(token.Id, token);
        }

        return token;
    }

    public async Task<Token?> GetByAccountId(Guid accountId, CancellationToken cancellationToken = default)
    {
        var token = await Storage.GetByAccountId(accountId, cancellationToken);

        if (token is not null)
        {
            Cache.Set(token.Id, token);
        }

        return token;
    }

    public async Task<Token> Create(Token value, CancellationToken cancellationToken = default)
    {
        var account = await Accounts.GetById(value.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var token = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            Tenant = tenant,
            Account = account,
            Token = token
        });

        return token;
    }

    public async Task<Token> Update(Token value, CancellationToken cancellationToken = default)
    {
        var account = await Accounts.GetById(value.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");
        var token = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            Tenant = tenant,
            Account = account,
            Token = token
        });

        return token;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await GetById(id, cancellationToken) ?? throw new Exception("token not found");
        var account = await Accounts.GetById(token.AccountId, cancellationToken) ?? throw new Exception("account not found");
        var tenant = await Tenants.GetById(account.TenantId, cancellationToken) ?? throw new Exception("tenant not found");

        await Storage.Delete(token.Id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            Tenant = tenant,
            Account = account,
            Token = token
        });
    }
}