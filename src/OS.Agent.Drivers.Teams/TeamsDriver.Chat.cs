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
        var activity = await Teams.Send(
            request.Chat.SourceId,
            new MessageActivity()
            {
                Text = request.Text,
                ReplyToId = request is MessageReplyRequest reply ? reply.ReplyTo.SourceId : null,
                Conversation = new()
                {
                    Id = request.Chat.SourceId,
                    Type = chatType,
                    Name = request.Chat.Name
                }
            }.AddAIGenerated().AddFeedback().ToMessage(),
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
                    Id = activity.Id,
                    Conversation = activity.Conversation,
                    Attachments = request.Attachments.Select(attachment => new Microsoft.Teams.Api.Attachment()
                    {
                        Id = attachment.Id,
                        Name = attachment.Name,
                        ContentType = new(attachment.ContentType),
                        Content = attachment.Content
                    }).AsList()
                }.AddAIGenerated().AddFeedback().ToMessage(),
                chatType,
                request.Chat.Url,
                cancellationToken
            );

            activity.Attachments = attachmentActivity.Attachments;
        }

        return new Message()
        {
            ChatId = request.Chat.Id,
            AccountId = request.Account.Id,
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

    public async Task<Message> Update(MessageUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var activity = request.Text is not null
            ? new MessageActivity()
            {
                Id = request.Message.SourceId,
                Text = request.Text,
            } : new MessageActivity()
            {
                Id = request.Message.SourceId,
                Attachments = request.Attachments?.Select(a => new Microsoft.Teams.Api.Attachment()
                {
                    ContentType = new Microsoft.Teams.Api.ContentType(a.ContentType),
                    Content = a.Content
                }).ToList()
            };

        await Teams.Send(
            request.Chat.SourceId,
            activity,
            new(request.Chat.Type ?? "personal"),
            request.Chat.Url,
            cancellationToken
        );

        if (!string.IsNullOrEmpty(request.Text))
        {
            request.Message.Text = request.Text;
        }

        if (request.Attachments is not null && request.Attachments.Any())
        {
            request.Message.Attachments = request.Attachments.ToList();
        }

        return request.Message;
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