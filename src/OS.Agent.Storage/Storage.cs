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
    IJobStorage Jobs { get; }
    IDocumentStorage Documents { get; }
}

public class Storage : IStorage
{
    public IAccountStorage Accounts { get; }
    public IChatStorage Chats { get; }
    public IMessageStorage Messages { get; }
    public ITenantStorage Tenants { get; }
    public IUserStorage Users { get; }
    public ITokenStorage Tokens { get; }
    public ILogStorage Logs { get; }
    public IRecordStorage Records { get; }
    public IInstallStorage Installs { get; }
    public IJobStorage Jobs { get; }
    public IDocumentStorage Documents { get; }

    public Storage(IServiceScopeFactory factory) : this(factory.CreateScope())
    {

    }

    public Storage(IServiceScope scope)
    {
        Accounts = scope.ServiceProvider.GetRequiredService<IAccountStorage>();
        Chats = scope.ServiceProvider.GetRequiredService<IChatStorage>();
        Messages = scope.ServiceProvider.GetRequiredService<IMessageStorage>();
        Tenants = scope.ServiceProvider.GetRequiredService<ITenantStorage>();
        Users = scope.ServiceProvider.GetRequiredService<IUserStorage>();
        Tokens = scope.ServiceProvider.GetRequiredService<ITokenStorage>();
        Logs = scope.ServiceProvider.GetRequiredService<ILogStorage>();
        Records = scope.ServiceProvider.GetRequiredService<IRecordStorage>();
        Installs = scope.ServiceProvider.GetRequiredService<IInstallStorage>();
        Jobs = scope.ServiceProvider.GetRequiredService<IJobStorage>();
        Documents = scope.ServiceProvider.GetRequiredService<IDocumentStorage>();
    }
}