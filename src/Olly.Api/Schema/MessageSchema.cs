using Olly.Services;

namespace Olly.Api.Schema;

[GraphQLName("Message")]
public class MessageSchema(Storage.Models.Message message) : ModelSchema
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = message.Id;

    [GraphQLName("source")]
    public SourceSchema Source { get; set; } = new(message.SourceId, message.SourceType, message.Url);

    [GraphQLName("text")]
    public string Text { get; set; } = message.Text;

    [GraphQLName("attachments")]
    public IEnumerable<AttachmentSchema> Attachments { get; set; } = message.Attachments.Select(attachment => new AttachmentSchema(attachment));

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = message.Entities.Select(entity => new EntitySchema(entity));

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = message.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = message.UpdatedAt;

    [GraphQLName("chat")]
    public async Task<ChatSchema> GetChat([Service] IChatService chatService, CancellationToken cancellationToken = default)
    {
        var chat = await chatService.GetById(message.ChatId, cancellationToken);

        if (chat is null)
        {
            throw new InvalidDataException();
        }

        return new(chat);
    }

    [GraphQLName("account")]
    public async Task<AccountSchema?> GetAccount([Service] IAccountService accountService, CancellationToken cancellationToken = default)
    {
        if (message.AccountId is null) return null;
        var account = await accountService.GetById(message.AccountId.Value, cancellationToken);
        return account is null ? null : new(account);
    }

    [GraphQLName("reply_to")]
    public async Task<MessageSchema?> GetReplyTo([Service] IMessageService messageService, CancellationToken cancellationToken = default)
    {
        if (message.ReplyToId is null) return null;
        var replyTo = await messageService.GetById(message.ReplyToId.Value, cancellationToken);
        return replyTo is null ? null : new(replyTo);
    }

    [GraphQLName("records")]
    public async Task<IEnumerable<RecordSchema>> GetRecords([Service] IRecordService recordService, CancellationToken cancellationToken = default)
    {
        var records = await recordService.GetByMessageId(Id, cancellationToken: cancellationToken);
        return records.List.Select(record => new RecordSchema(record));
    }

    [GraphQLName("jobs")]
    public async Task<IEnumerable<JobSchema>> GetJobs([Service] IJobService jobService, CancellationToken cancellationToken = default)
    {
        var jobs = await jobService.GetByMessageId(Id, cancellationToken: cancellationToken);
        return jobs.List.Select(job => new JobSchema(job));
    }

    [GraphQLName("logs")]
    public async Task<IEnumerable<LogSchema>> GetLogs([Service] IServices services, CancellationToken cancellationToken = default)
    {
        var chat = await services.Chats.GetById(message.ChatId, cancellationToken) ?? throw new Exception("chat not found");
        var res = await services.Logs.GetByTypeId(chat.TenantId, Storage.Models.LogType.Message, message.Id.ToString(), cancellationToken: cancellationToken);
        return res.List.Select(log => new LogSchema(log));
    }
}