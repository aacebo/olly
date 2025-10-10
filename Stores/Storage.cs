namespace OS.Agent.Stores;

public interface IStorage
{
    IAccountStorage Accounts { get; }
    IChatStorage Chats { get; }
    IEntityStorage Entities { get; }
    IMessageStorage Messages { get; }
    ITenantStorage Tenants { get; }
    IUserStorage Users { get; }
}

public class Storage : IStorage
{
    public IAccountStorage Accounts => _accounts;
    public IChatStorage Chats => _chats;
    public IEntityStorage Entities => _entities;
    public IMessageStorage Messages => _messages;
    public ITenantStorage Tenants => _tenants;
    public IUserStorage Users => _users;

    private readonly IAccountStorage _accounts;
    private readonly IChatStorage _chats;
    private readonly IEntityStorage _entities;
    private readonly IMessageStorage _messages;
    private readonly ITenantStorage _tenants;
    private readonly IUserStorage _users;

    public Storage(IServiceScopeFactory factory) : this(factory.CreateScope())
    {

    }

    public Storage(IServiceScope scope)
    {
        _accounts = scope.ServiceProvider.GetRequiredService<IAccountStorage>();
        _chats = scope.ServiceProvider.GetRequiredService<IChatStorage>();
        _entities = scope.ServiceProvider.GetRequiredService<IEntityStorage>();
        _messages = scope.ServiceProvider.GetRequiredService<IMessageStorage>();
        _tenants = scope.ServiceProvider.GetRequiredService<ITenantStorage>();
        _users = scope.ServiceProvider.GetRequiredService<IUserStorage>();
    }
}