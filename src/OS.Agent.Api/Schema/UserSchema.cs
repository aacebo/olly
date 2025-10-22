namespace OS.Agent.Api.Schema;

[GraphQLName("User")]
public class UserSchema(Storage.Models.User user) : ModelSchema
{
    [GraphQLName("id")]
    public Guid Id { get; set; } = user.Id;

    [GraphQLName("name")]
    public string? Name { get; set; } = user.Name;

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = user.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = user.UpdatedAt;
}