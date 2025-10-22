using OS.Agent.Services;

namespace OS.Agent.Api.Schema;

[GraphQLName("Log")]
public class LogSchema(Storage.Models.Log log)
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = log.Id;

    [GraphQLName("level")]
    public string Level { get; set; } = log.Level;

    [GraphQLName("type")]
    public string Type { get; set; } = log.Type;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = log.Entities.Select(entity => new EntitySchema(entity));

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = log.CreatedAt;

    [GraphQLName("tenant")]
    public async Task<TenantSchema> GetTenant([Service] ITenantService tenantService, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantService.GetById(log.TenantId, cancellationToken);

        if (tenant is null)
        {
            throw new InvalidDataException();
        }

        return new(tenant);
    }

    [GraphQLName("subject")]
    public async Task<ModelSchema?> GetSubject([Service] IAccountService accountService, [Service] IChatService chatService, [Service] IMessageService messageService, CancellationToken cancellationToken = default)
    {
        if (log.TypeId is null) return null;
        if (log.Type == Storage.Models.LogType.Account)
        {
            var value = await accountService.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new AccountSchema(value);
        }
        else if (log.Type == Storage.Models.LogType.Chat)
        {
            var value = await chatService.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new ChatSchema(value);
        }
        else if (log.Type == Storage.Models.LogType.Message)
        {
            var value = await messageService.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new MessageSchema(value);
        }

        return null;
    }
}