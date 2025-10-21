using OS.Agent.Services;

namespace OS.Agent.Api.Schema;

[GraphQLName("Tenant")]
public class TenantSchema(Storage.Models.Tenant tenant)
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = tenant.Id;

    [GraphQLName("sources")]
    public IList<Storage.Models.Source> Sources { get; set; } = tenant.Sources;

    [GraphQLName("name")]
    public string? Name { get; set; } = tenant.Name;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = tenant.Entities.Select(account => new EntitySchema(account));

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
}