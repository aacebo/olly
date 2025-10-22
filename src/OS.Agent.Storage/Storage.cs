using Microsoft.Extensions.DependencyInjection;

namespace OS.Agent.Storage;

public interface IStorage
{
    IAccountStorage Accounts { get; }
    IChatStorage Chats { get; }
    IMessageStorage Messages { get; }
    ITenantStorage Tenants { get; }
    IUserStorage Users { get; }
    ITokenStorage Tokens { get; }
    ILogStorage Logs { get; }
    IRecordStorage Records { get; }
    IInstallStorage Installs { get; }
}

public class Storage : IStorage
{
    public IAccountStorage Accounts => _accounts;
    public IChatStorage Chats => _chats;
    public IMessageStorage Messages => _messages;
    public ITenantStorage Tenants => _tenants;
    public IUserStorage Users => _users;
    public ITokenStorage Tokens => _tokens;
    public ILogStorage Logs => _logs;
    public IRecordStorage Records => _records;
    public IInstallStorage Installs => _installs;

    private readonly IAccountStorage _accounts;
    private readonly IChatStorage _chats;
    private readonly IMessageStorage _messages;
    private readonly ITenantStorage _tenants;
    private readonly IUserStorage _users;
    private readonly ITokenStorage _tokens;
    private readonly ILogStorage _logs;
    private readonly IRecordStorage _records;
    private readonly IInstallStorage _installs;

    public Storage(IServiceScopeFactory factory) : this(factory.CreateScope())
    {

    }

    public Storage(IServiceScope scope)
    {
        _accounts = scope.ServiceProvider.GetRequiredService<IAccountStorage>();
        _chats = scope.ServiceProvider.GetRequiredService<IChatStorage>();
        _messages = scope.ServiceProvider.GetRequiredService<IMessageStorage>();
        _tenants = scope.ServiceProvider.GetRequiredService<ITenantStorage>();
        _users = scope.ServiceProvider.GetRequiredService<IUserStorage>();
        _tokens = scope.ServiceProvider.GetRequiredService<ITokenStorage>();
        _logs = scope.ServiceProvider.GetRequiredService<ILogStorage>();
        _records = scope.ServiceProvider.GetRequiredService<IRecordStorage>();
        _installs = scope.ServiceProvider.GetRequiredService<IInstallStorage>();
    }
}