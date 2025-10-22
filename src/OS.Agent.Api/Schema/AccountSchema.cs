using OS.Agent.Services;

namespace OS.Agent.Api.Schema;

[GraphQLName("Account")]
public class AccountSchema(Storage.Models.Account account) : ModelSchema
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = account.Id;

    [GraphQLName("source")]
    public SourceSchema Source { get; set; } = new(account.SourceId, account.SourceType, account.Url);

    [GraphQLName("name")]
    public string? Name { get; set; } = account.Name;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = account.Entities.Select(entity => new EntitySchema(entity));

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = account.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = account.UpdatedAt;

    [GraphQLName("tenant")]
    public async Task<TenantSchema> GetTenant([Service] ITenantService tenantService, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantService.GetById(account.TenantId, cancellationToken);

        if (tenant is null)
        {
            throw new InvalidDataException();
        }

        return new(tenant);
    }

    [GraphQLName("user")]
    public async Task<UserSchema?> GetUser([Service] IUserService userService, CancellationToken cancellationToken = default)
    {
        if (account.UserId is null) return null;
        var user = await userService.GetById(account.UserId.Value, cancellationToken);
        return user is null ? null : new(user);
    }

    [GraphQLName("install")]
    public async Task<InstallSchema?> GetInstall([Service] IInstallService installService, CancellationToken cancellationToken = default)
    {
        var install = await installService.GetByAccountId(Id, cancellationToken);
        return install is null ? null : new(install);
    }
}