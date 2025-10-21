using OS.Agent.Services;

namespace OS.Agent.Api.Schema;

[GraphQLName("Account")]
public class AccountSchema(Storage.Models.Account account)
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = account.Id;

    [GraphQLName("source_type")]
    [GraphQLType(typeof(EnumSchema<Storage.Models.SourceType>))]
    public Storage.Models.SourceType SourceType { get; set; } = account.SourceType;

    [GraphQLName("source_id")]
    public string SourceId { get; set; } = account.SourceId;

    [GraphQLName("name")]
    public string? Name { get; set; } = account.Name;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = account.Entities.Select(account => new EntitySchema(account));

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
}