using Olly.Services;
using Olly.Storage;

namespace Olly.Api.Schema;

[GraphQLName("Chat")]
public class ChatSchema(Storage.Models.Chat chat) : ModelSchema
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = chat.Id;

    [GraphQLName("source")]
    public SourceSchema Source { get; set; } = new(chat.SourceId, chat.SourceType, chat.Url);

    [GraphQLName("type")]
    public string? Type { get; set; } = chat.Type;

    [GraphQLName("name")]
    public string? Name { get; set; } = chat.Name;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = chat.Entities.Select(entity => new EntitySchema(entity));

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

    [GraphQLName("messages")]
    public async Task<IEnumerable<MessageSchema>> GetMessages([Service] IMessageService messageService, CancellationToken cancellationToken = default)
    {
        var messages = await messageService.GetByChatId(
            Id,
            Page.Create()
                .Sort(SortDirection.Desc, "created_at")
                .Build(),
            cancellationToken
        );

        return messages.List.Select(message => new MessageSchema(message));
    }

    [GraphQLName("records")]
    public async Task<IEnumerable<RecordSchema>> GetRecords([Service] IRecordService recordService, CancellationToken cancellationToken = default)
    {
        var records = await recordService.GetByChatId(Id, cancellationToken: cancellationToken);
        return records.List.Select(record => new RecordSchema(record));
    }

    [GraphQLName("jobs")]
    public async Task<IEnumerable<JobSchema>> GetJobs([Service] IJobService jobService, CancellationToken cancellationToken = default)
    {
        var jobs = await jobService.GetByChatId(Id, cancellationToken: cancellationToken);
        return jobs.List.Select(job => new JobSchema(job));
    }

    [GraphQLName("logs")]
    public async Task<IEnumerable<LogSchema>> GetLogs([Service] IServices services, CancellationToken cancellationToken = default)
    {
        var res = await services.Logs.GetByTypeId(chat.TenantId, Storage.Models.LogType.Chat, chat.Id.ToString(), cancellationToken: cancellationToken);
        return res.List.Select(log => new LogSchema(log));
    }
}