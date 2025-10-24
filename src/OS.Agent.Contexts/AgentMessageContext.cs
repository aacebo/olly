using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

using OS.Agent.Cards.Extensions;
using OS.Agent.Cards.Progress;
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

    private Message? ProgressMessage { get; set; }
    private IList<(ProgressStyle, string)> ProgressHistory { get; set; } = [];

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
        if (ProgressMessage is null)
        {
            ProgressMessage = await Send(text);
            return ProgressMessage;
        }

        ProgressMessage = await Update(ProgressMessage.Id, text);
        return ProgressMessage;
    }

    public async Task<Message> Progress(params Attachment[] attachments)
    {
        if (ProgressMessage is null)
        {
            ProgressMessage = await Send("please wait...");
        }

        ProgressMessage = await Update(ProgressMessage.Id, attachments);
        return ProgressMessage;
    }

    public async Task<Message> Progress(string text, params Attachment[] attachments)
    {
        if (ProgressMessage is null)
        {
            ProgressMessage = await Send(text);
        }

        ProgressMessage = await Update(ProgressMessage.Id, attachments);
        return ProgressMessage;
    }

    public async Task SendProgressUpdate(string style, string? title = null, string? message = null)
    {
        var progressStyle = new ProgressStyle(style);

        if (!(progressStyle.IsInProgress || progressStyle.IsSuccess || progressStyle.IsWarning || progressStyle.IsError))
        {
            throw new InvalidOperationException("invalid style, supported values are 'in-progress', 'success', 'warning', 'error'");
        }

        if (ProgressMessage is null && (progressStyle.IsSuccess || progressStyle.IsWarning || progressStyle.IsError))
        {
            return;
        }

        if (message is not null)
        {
            ProgressHistory.Add((progressStyle, message));
        }

        var card = new ProgressCard(progressStyle);

        if (title is not null)
        {
            card = card.AddHeader(title);
        }

        card = card
            .AddProgressBar(progressStyle.IsInProgress ? null : 100)
            .AddFooter(message);

        card = (ProgressCard)card.WithStyle(progressStyle.ContainerStyle) ?? throw new InvalidDataException();
        card.Body?.Add(
            new ActionSet(
                new ShowCardAction()
                    .WithTitle($"Show {ProgressHistory.Count}")
                    .WithTooltip("Task Status Updates")
                    .WithCard(new AdaptiveCard(
                        ProgressHistory
                            .Select(item =>
                                new ColumnSet()
                                .WithColumns(
                                    new Column(
                                        new Icon(item.Item1.Icon)
                                            .WithColor(item.Item1.Color)
                                            .WithSize(IconSize.XxSmall)
                                    )
                                    .WithWidth(new Union<string, float>("auto"))
                                    .WithVerticalContentAlignment(VerticalAlignment.Center),
                                    new Column(
                                        new TextBlock(item.Item2)
                                            .WithColor(item.Item1.Color)
                                            .WithSize(TextSize.Small)
                                            .WithSpacing(Spacing.Small)
                                            .WithIsSubtle(true)
                                            .WithWrap(false)
                                    )
                                    .WithWidth(new Union<string, float>("stetch"))
                                    .WithVerticalContentAlignment(VerticalAlignment.Center)
                                )
                                .WithShowBorder(true)
                                .WithRoundedCorners(true)
                            )
                            .ToList<CardElement>()
                        )
                    )
            )
            .WithHorizontalAlignment(HorizontalAlignment.Right)
        );

        var attachment = card.ToAttachment();

        await Progress(new Attachment()
        {
            ContentType = attachment.ContentType,
            Content = attachment.Content ?? throw new JsonException()
        });

        await Typing();
    }
}