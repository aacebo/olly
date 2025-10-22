using Dapper;

using Microsoft.Teams.Api.Activities;

using OS.Agent.Drivers.Models;
using OS.Agent.Drivers.Teams.Models;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsDriver
{
    public async Task Typing(TypingRequest request, CancellationToken cancellationToken = default)
    {
        var chatType = request.Chat.Type is null ? Microsoft.Teams.Api.ConversationType.Personal : new(request.Chat.Type);

        await Teams.Send(
            request.Chat.SourceId,
            new TypingActivity()
            {
                Text = request.Text,
                Conversation = new()
                {
                    Id = request.Chat.SourceId,
                    Type = chatType,
                    Name = request.Chat.Name
                }
            },
            chatType,
            request.Chat.Url,
            cancellationToken
        );
    }

    public async Task<Message> Send(MessageRequest request, CancellationToken cancellationToken = default)
    {
        var chatType = request.Chat.Type is null ? Microsoft.Teams.Api.ConversationType.Personal : new(request.Chat.Type);
        var activity = new MessageActivity()
        {
            Text = request.Text,
            ReplyToId = request is MessageReplyRequest reply ? reply.ReplyTo.SourceId : null,
            Conversation = new()
            {
                Id = request.Chat.SourceId,
                Type = chatType,
                Name = request.Chat.Name
            }
        };

        activity = await Teams.Send(
            request.Chat.SourceId,
            activity,
            chatType,
            request.Chat.Url,
            cancellationToken
        );

        if (request.Attachments.Any())
        {
            var attachmentActivity = await Teams.Send(
                request.Chat.SourceId,
                new MessageActivity()
                {
                    ReplyToId = activity.ReplyToId,
                    Conversation = activity.Conversation,
                    Attachments = request.Attachments.Select(attachment => new Microsoft.Teams.Api.Attachment()
                    {
                        Id = attachment.Id,
                        Name = attachment.Name,
                        ContentType = new(attachment.ContentType),
                        Content = attachment.Content
                    }).AsList()
                },
                chatType,
                request.Chat.Url,
                cancellationToken
            );

            activity.Attachments = attachmentActivity.Attachments;
        }

        return new Message()
        {
            ChatId = request.Chat.Id,
            AccountId = request.From.Id,
            SourceId = activity.Id,
            SourceType = SourceType.Teams,
            Url = $"{request.Chat.Url}v3/conversations/{request.Chat.SourceId}/activities/{activity.Id}",
            Text = activity.Text,
            Attachments = request.Attachments.ToList(),
            Entities = [
                new TeamsMessageEntity()
                {
                    Activity = activity
                }
            ]
        };
    }

    public async Task<Message> Reply(MessageReplyRequest request, CancellationToken cancellationToken = default)
    {
        var replyTo = request.ReplyTo.Entities.GetRequired<TeamsMessageEntity>();

        request.Text = string.Join("\n", [
            replyTo.Activity.ToQuoteReply(),
            request.Text != string.Empty ? $"<p>{request.Text}</p>" : string.Empty
        ]);

        var message = await Send(request, cancellationToken);
        message.ReplyToId = request.ReplyTo.Id;
        return message;
    }
}