using Microsoft.Teams.Api.Activities;

using OS.Agent.Drivers.Teams.Events;
using OS.Agent.Drivers.Teams.Models;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsClient
{
    public override async Task Typing(string? text = null)
    {
        var chatType = Event.Chat.Type is null ? Microsoft.Teams.Api.ConversationType.Personal : new(Event.Chat.Type);

        await Teams.Send(
            Event.Chat.SourceId,
            new TypingActivity()
            {
                Text = text,
                Conversation = new()
                {
                    Id = Event.Chat.SourceId,
                    Type = chatType,
                    Name = Event.Chat.Name
                }
            },
            chatType,
            Event.Chat.Url,
            CancellationToken
        );
    }

    public override async Task<Message> Send(string text)
    {
        return await Send(text, []);
    }

    public override async Task<Message> Send(params Attachment[] attachments)
    {
        return await Send(string.Empty, []);
    }

    public override async Task<Message> Send(string text, params Attachment[] attachments)
    {
        var chatType = Event.Chat.Type is null ? Microsoft.Teams.Api.ConversationType.Personal : new(Event.Chat.Type);
        var activity = await Send(
            new MessageActivity()
            {
                Text = text,
                Conversation = new()
                {
                    Id = Event.Chat.SourceId,
                    Type = chatType,
                    Name = Event.Chat.Name
                },
                Attachments = attachments.Select(attachment => new Microsoft.Teams.Api.Attachment()
                {
                    Id = attachment.Id,
                    Name = attachment.Name,
                    ContentType = new(attachment.ContentType),
                    Content = attachment.Content
                }).ToList()
            }.AddAIGenerated().AddFeedback().ToMessage()
        );

        var message = new Message()
        {
            ChatId = Event.Chat.Id,
            AccountId = Event.Account.Id,
            SourceId = activity.Id,
            SourceType = SourceType.Teams,
            Url = $"{Event.Chat.Url}v3/conversations/{Event.Chat.SourceId}/activities/{activity.Id}",
            Text = activity.Text,
            Attachments = attachments.ToList(),
            Entities = [
                new TeamsMessageEntity()
                {
                    Activity = activity
                }
            ]
        };

        return string.IsNullOrEmpty(activity.Id)
            ? message
            : await Storage.Messages.Create(message, cancellationToken: CancellationToken);
    }

    public override async Task<Message> SendUpdate(Guid id, string? text, params Attachment[] attachments)
    {
        var message = await Services.Messages.GetById(id, CancellationToken) ?? throw new Exception("message not found");
        var activity = !string.IsNullOrEmpty(text)
            ? new MessageActivity()
            {
                Id = message.SourceId,
                Text = text,
            } : new MessageActivity()
            {
                Id = message.SourceId,
                Attachments = attachments.Select(a => new Microsoft.Teams.Api.Attachment()
                {
                    ContentType = new Microsoft.Teams.Api.ContentType(a.ContentType),
                    Content = a.Content
                }).ToList()
            };

        await Teams.Send(
            Event.Chat.SourceId,
            activity,
            new(Event.Chat.Type ?? "personal"),
            Event.Chat.Url,
            CancellationToken
        );

        if (!string.IsNullOrEmpty(text))
        {
            message.Text = text;
        }

        if (attachments.Length != 0)
        {
            message.Attachments = attachments.ToList();
        }

        return await Storage.Messages.Update(message, cancellationToken: CancellationToken);
    }

    public override async Task<Message> SendReply(string text, params Attachment[] attachments)
    {
        if (Event is not TeamsMessageEvent messageEvent) throw new InvalidOperationException("no message to reply to");

        var replyTo = messageEvent.Message.Entities.GetRequired<TeamsMessageEntity>();

        text = string.Join("\n", [
            replyTo.Activity.ToQuoteReply(),
            text != string.Empty ? $"<p>{text}</p>" : string.Empty
        ]);

        var chatType = messageEvent.Chat.Type is null ? Microsoft.Teams.Api.ConversationType.Personal : new(messageEvent.Chat.Type);
        var activity = await Send(
            new MessageActivity()
            {
                Text = text,
                ReplyToId = replyTo.Activity.Id,
                Conversation = new()
                {
                    Id = messageEvent.Chat.SourceId,
                    Type = chatType,
                    Name = messageEvent.Chat.Name
                },
                Attachments = attachments.Select(attachment => new Microsoft.Teams.Api.Attachment()
                {
                    Id = attachment.Id,
                    Name = attachment.Name,
                    ContentType = new(attachment.ContentType),
                    Content = attachment.Content
                }).ToList()
            }.AddAIGenerated().AddFeedback().ToMessage()
        );

        var message = new Message()
        {
            ChatId = messageEvent.Chat.Id,
            AccountId = messageEvent.Account.Id,
            ReplyToId = messageEvent.Message.Id,
            SourceId = activity.Id,
            SourceType = SourceType.Teams,
            Url = $"{messageEvent.Chat.Url}v3/conversations/{messageEvent.Chat.SourceId}/activities/{activity.Id}",
            Text = activity.Text,
            Attachments = attachments.ToList(),
            Entities = [
                new TeamsMessageEntity()
                {
                    Activity = activity
                }
            ]
        };

        return string.IsNullOrEmpty(activity.Id)
            ? message
            : await Storage.Messages.Create(message, cancellationToken: CancellationToken);
    }

    public async Task<MessageActivity> Send(MessageActivity activity)
    {
        var chatType = Event.Chat.Type is null
            ? Microsoft.Teams.Api.ConversationType.Personal
            : new(Event.Chat.Type);

        var attachments = activity.Attachments?.ToList();

        activity.Attachments = null;
        activity = await Teams.Send(
            Event.Chat.SourceId,
            activity,
            chatType,
            Event.Chat.Url,
            CancellationToken
        );

        if (attachments is not null && attachments.Count != 0)
        {
            await Teams.Send(
                Event.Chat.SourceId,
                new MessageActivity()
                {
                    Id = activity.Id,
                    Conversation = activity.Conversation,
                    Attachments = attachments
                }.AddAIGenerated().AddFeedback().ToMessage(),
                chatType,
                Event.Chat.Url,
                CancellationToken
            );

            activity.Attachments = attachments;
        }

        return activity;
    }
}