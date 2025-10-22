using OS.Agent.Services;

namespace OS.Agent.Api.Schema;

[GraphQLName("Install")]
public class InstallSchema(Storage.Models.Install install)
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = install.Id;

    [GraphQLName("source")]
    public SourceSchema Source { get; set; } = new(install.SourceId, install.SourceType, install.Url);

    [GraphQLName("access_token")]
    public string? AccessToken { get; set; } = install.AccessToken;

    [GraphQLName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; } = install.ExpiresAt;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = install.Entities.Select(entity => new EntitySchema(entity));

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = install.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; } = install.UpdatedAt;

    [GraphQLName("account")]
    public async Task<AccountSchema> GetAccount([Service] IAccountService accountService, CancellationToken cancellationToken = default)
    {
        var account = await accountService.GetById(install.AccountId, cancellationToken);

        if (account is null)
        {
            throw new InvalidDataException();
        }

        return new(account);
    }
}