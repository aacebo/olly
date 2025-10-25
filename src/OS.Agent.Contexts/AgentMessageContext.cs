using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Cards.Extensions;
using OS.Agent.Cards.Progress;
using OS.Agent.Cards.Tasks;
using OS.Agent.Drivers;
using OS.Agent.Drivers.Models;
using OS.Agent.Storage.Models;

namespace OS.Agent.Contexts;

/// <summary>
/// The context created to handle agent message events
/// </summary>
public class AgentMessageContext : AgentContext<IChatDriver>
{
    public required Tenant Tenant { get; init; }
    public required Account Account { get; init; }
    public required User User { get; init; }
    public required Install Installation { get; init; }
    public required Chat Chat { get; init; }
    public required Message Message { get; init; }
    public IList<TaskItem> Tasks => Response.TaskCard.Tasks;

    private AgentResponse Response { get; } = new();

    public AgentMessageContext(SourceType type, IServiceScopeFactory factory) : base(type, factory)
    {

    }

    public AgentMessageContext(SourceType type, IServiceScope scope, CancellationToken cancellationToken = default) : base(type, scope, cancellationToken)
    {

    }

    public async Task SignIn(string url, string state)
    {
        await Driver.SignIn(new()
        {
            Chat = Chat,
            From = Account,
            Url = url,
            State = state
        }, CancellationToken);
    }

    public async Task Typing(string? text = null)
    {
        var request = new TypingRequest()
        {
            Text = text,
            Chat = Chat,
            From = Account,
            Install = Installation
        };

        await Driver.Typing(request, CancellationToken);
    }

    public async Task<Message> Send(string text, params Attachment[] attachments)
    {
        var request = new MessageRequest()
        {
            Text = text,
            Attachments = attachments,
            Chat = Chat,
            From = Account,
            Install = Installation
        };

        var message = await Driver.Send(request, CancellationToken);
        if (string.IsNullOrEmpty(message.SourceId)) return message;
        return await Storage.Messages.Create(message, cancellationToken: CancellationToken);
    }

    public async Task<Message> Update(Guid id, string text, params Attachment[] attachments)
    {
        var message = await Services.Messages.GetById(id, CancellationToken) ?? throw new Exception("message not found");
        var request = new MessageUpdateRequest()
        {
            Text = text,
            Attachments = attachments.Length > 0 ? attachments : null,
            Chat = Chat,
            From = Account,
            Install = Installation,
            Message = message
        };

        message = await Driver.Update(request, CancellationToken);
        if (string.IsNullOrEmpty(message.SourceId)) return message;
        return await Storage.Messages.Update(message, cancellationToken: CancellationToken);
    }

    public async Task<Message> Update(Guid id, params Attachment[] attachments)
    {
        var message = await Services.Messages.GetById(id, CancellationToken) ?? throw new Exception("message not found");
        var request = new MessageUpdateRequest()
        {
            Attachments = attachments.Length > 0 ? attachments : null,
            Chat = Chat,
            From = Account,
            Install = Installation,
            Message = message
        };

        message = await Driver.Update(request, CancellationToken);
        if (string.IsNullOrEmpty(message.SourceId)) return message;
        return await Storage.Messages.Update(message, cancellationToken: CancellationToken);
    }

    public async Task<Message> Reply(string text, params Attachment[] attachments)
    {
        var request = new MessageReplyRequest()
        {
            Text = text,
            Attachments = attachments,
            Chat = Chat,
            Install = Installation,
            From = Account,
            ReplyTo = Message,
            ReplyToAccount = Account,
        };

        var message = await Driver.Reply(request, CancellationToken);
        if (string.IsNullOrEmpty(message.SourceId)) return message;
        return await Storage.Messages.Create(message, cancellationToken: CancellationToken);
    }

    public async Task<Message> Progress(string text)
    {
        if (Response.Progress is null)
        {
            Response.Progress = await Send(text);
            return Response.Progress;
        }

        Response.Progress = await Update(Response.Progress.Id, text);
        return Response.Progress;
    }

    public async Task<Message> Progress(params Attachment[] attachments)
    {
        if (Response.Progress is null)
        {
            Response.Progress = await Send("please wait...");
        }

        Response.Progress = await Update(Response.Progress.Id, attachments);
        return Response.Progress;
    }

    public async Task<Message> Progress(string text, params Attachment[] attachments)
    {
        if (Response.Progress is null)
        {
            Response.Progress = await Send(text);
        }

        Response.Progress = await Update(Response.Progress.Id, attachments);
        return Response.Progress;
    }

    public async Task<TaskItem> CreateTask(TaskItem.Create create)
    {
        var task = Response.TaskCard.Add(create);
        var attachment = Response.TaskCard.Render().ToAttachment();

        await Progress(new Attachment()
        {
            ContentType = attachment.ContentType,
            Content = attachment.Content ?? throw new JsonException()
        });

        await Typing();
        return task;
    }

    public async Task<TaskItem> UpdateTask(Guid id, TaskItem.Update update)
    {
        var task = Response.TaskCard.Update(id, update);
        var attachment = Response.TaskCard.Render().ToAttachment();

        await Progress(new Attachment()
        {
            ContentType = attachment.ContentType,
            Content = attachment.Content ?? throw new JsonException()
        });

        await Typing();
        return task;
    }

    public async Task<TaskItem> Finish()
    {
        var errors = Response.TaskCard.Tasks.Count(t => t.Style.IsError);
        var warnings = Response.TaskCard.Tasks.Count(t => t.Style.IsWarning);
        var task = Response.TaskCard.Current is not null
            ? Response.TaskCard.Current
            : Response.TaskCard.Add(new()
            {
                Style = ProgressStyle.InProgress,
                Title = "✅ Done!",
                Message = "Success!"
            });

        task = Response.TaskCard.Update(task.Id, new()
        {
            Style = errors + warnings > 0 ? ProgressStyle.Warning : ProgressStyle.Success,
            EndedAt = DateTimeOffset.UtcNow
        });

        var attachment = Response.TaskCard.Render().ToAttachment();

        await Progress(new Attachment()
        {
            ContentType = attachment.ContentType,
            Content = attachment.Content ?? throw new JsonException()
        });

        await Typing();
        return task;
    }
}