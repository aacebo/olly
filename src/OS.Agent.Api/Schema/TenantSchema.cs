using OS.Agent.Services;

namespace OS.Agent.Api.Schema;

[GraphQLName("Tenant")]
public class TenantSchema(Storage.Models.Tenant tenant)
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = tenant.Id;

    [GraphQLName("sources")]
    public IEnumerable<SourceSchema> Sources { get; set; } = tenant.Sources.Select(src => new SourceSchema(src));

    [GraphQLName("name")]
    public string? Name { get; set; } = tenant.Name;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = tenant.Entities.Select(entity => new EntitySchema(entity));

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = tenant.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = tenant.UpdatedAt;

    [GraphQLName("accounts")]
    public async Task<IEnumerable<AccountSchema>> GetAccounts([Service] IAccountService accountService, CancellationToken cancellationToken = default)
    {
        var accounts = await accountService.GetByTenantId(Id, cancellationToken);
        return accounts.Select(account => new AccountSchema(account));
    }

    [GraphQLName("chats")]
    public async Task<IEnumerable<ChatSchema>> GetChats([Service] IChatService chatService, CancellationToken cancellationToken = default)
    {
        var chats = await chatService.GetByTenantId(Id, cancellationToken: cancellationToken);
        return chats.List.Select(chat => new ChatSchema(chat));
    }

    [GraphQLName("records")]
    public async Task<IEnumerable<RecordSchema>> GetRecords([Service] IRecordService recordService, CancellationToken cancellationToken = default)
    {
        var records = await recordService.GetByTenantId(Id, cancellationToken: cancellationToken);
        return records.List.Select(record => new RecordSchema(record));
    }
}