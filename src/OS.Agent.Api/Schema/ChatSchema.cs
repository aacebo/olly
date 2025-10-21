using OS.Agent.Services;

namespace OS.Agent.Api.Schema;

[GraphQLName("Chat")]
public class ChatSchema(Storage.Models.Chat chat)
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = chat.Id;

    [GraphQLName("source_type")]
    public string SourceType { get; set; } = chat.SourceType;

    [GraphQLName("source_id")]
    public string SourceId { get; set; } = chat.SourceId;

    [GraphQLName("type")]
    public string? Type { get; set; } = chat.Type;

    [GraphQLName("name")]
    public string? Name { get; set; } = chat.Name;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = chat.Entities.Select(account => new EntitySchema(account));

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = chat.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = chat.UpdatedAt;

    [GraphQLName("tenant")]
    public async Task<TenantSchema> GetTenant([Service] ITenantService tenantService, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantService.GetById(chat.TenantId, cancellationToken);

        if (tenant is null)
        {
            throw new InvalidDataException();
        }

        return new(tenant);
    }

    [GraphQLName("parent")]
    public async Task<ChatSchema?> GetParent([Service] IChatService chatService, CancellationToken cancellationToken = default)
    {
        if (chat.ParentId is null) return null;
        var parent = await chatService.GetById(chat.ParentId.Value, cancellationToken);
        return parent is null ? null : new(parent);
    }

    [GraphQLName("chats")]
    public async Task<IEnumerable<ChatSchema>> GetChats([Service] IChatService chatService, CancellationToken cancellationToken = default)
    {
        var chats = await chatService.GetByParentId(Id, cancellationToken);
        return chats.Select(chat => new ChatSchema(chat));
    }
}