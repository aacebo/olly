using Microsoft.Extensions.DependencyInjection;

namespace Olly.Services;

public interface IServices
{
    IAccountService Accounts { get; }
    IChatService Chats { get; }
    IMessageService Messages { get; }
    ITenantService Tenants { get; }
    IUserService Users { get; }
    ITokenService Tokens { get; }
    ILogService Logs { get; }
    IRecordService Records { get; }
    IInstallService Installs { get; }
    IDocumentService Documents { get; }
    IJobService Jobs { get; }
    IJobApprovalService Approvals { get; }
    IJobRunService Runs { get; }
}

public class Services : IServices
{
    public IAccountService Accounts { get; }
    public IChatService Chats { get; }
    public IMessageService Messages { get; }
    public ITenantService Tenants { get; }
    public IUserService Users { get; }
    public ITokenService Tokens { get; }
    public ILogService Logs { get; }
    public IRecordService Records { get; }
    public IInstallService Installs { get; }
    public IDocumentService Documents { get; }
    public IJobService Jobs { get; }
    public IJobApprovalService Approvals { get; }
    public IJobRunService Runs { get; }

    public Services(IServiceScopeFactory factory) : this(factory.CreateScope())
    {

    }

    public Services(IServiceScope scope)
    {
        Accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        Chats = scope.ServiceProvider.GetRequiredService<IChatService>();
        Messages = scope.ServiceProvider.GetRequiredService<IMessageService>();
        Tenants = scope.ServiceProvider.GetRequiredService<ITenantService>();
        Users = scope.ServiceProvider.GetRequiredService<IUserService>();
        Tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        Logs = scope.ServiceProvider.GetRequiredService<ILogService>();
        Records = scope.ServiceProvider.GetRequiredService<IRecordService>();
        Installs = scope.ServiceProvider.GetRequiredService<IInstallService>();
        Documents = scope.ServiceProvider.GetRequiredService<IDocumentService>();
        Jobs = scope.ServiceProvider.GetRequiredService<IJobService>();
        Approvals = scope.ServiceProvider.GetRequiredService<IJobApprovalService>();
        Runs = scope.ServiceProvider.GetRequiredService<IJobRunService>();
    }
}