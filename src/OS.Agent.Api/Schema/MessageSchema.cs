using OS.Agent.Services;

namespace OS.Agent.Api.Schema;

[GraphQLName("Message")]
public class MessageSchema(Storage.Models.Message message)
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = message.Id;

    [GraphQLName("source")]
    public SourceSchema Source { get; set; } = new(message.SourceId, message.SourceType);

    [GraphQLName("text")]
    public string Text { get; set; } = message.Text;

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
}