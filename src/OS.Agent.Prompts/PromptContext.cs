using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

using OS.Agent.Cards.Extensions;
using OS.Agent.Cards.Progress;
using OS.Agent.Drivers;
using OS.Agent.Drivers.Models;
using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Prompts;

public interface IPromptContext
{
    Guid UserId { get; }
    Octokit.GitHubClient AppGithub { get; }
    OpenAIChatModel Model { get; }
    Tenant Tenant { get; }
    Account Account { get; }
    Install Install { get; }
    Chat Chat { get; }
    Message Message { get; }
    ITenantService Tenants { get; }
    IAccountService Accounts { get; }
    IInstallService Installs { get; }
    IChatService Chats { get; }
    IMessageService Messages { get; }
    ITokenService Tokens { get; }
    IRecordService Records { get; }
    IChatDriver Driver { get; }
    CancellationToken CancellationToken { get; }
    IServiceProvider Services { get; }

    Task SignIn(string url, string state);
    Task Typing(string? text = null);
    Task<Message> Send(string text, params Attachment[] attachments);
    Task<Message> Update(Guid id, string text, params Attachment[] attachments);
    Task<Message> Update(Guid id, params Attachment[] attachments);
    Task<Message> Reply(string text, params Attachment[] attachments);
    Task<Message> Progress(string text);
    Task<Message> Progress(params Attachment[] attachments);
    Task<Message> Progress(string text, params Attachment[] attachments);
    Task SendProgressUpdate(string style, string? title = null, string? message = null);
}

public class PromptContext : IPromptContext
{
    public Guid UserId => Account.UserId ?? throw new InvalidDataException("Accounts sending messages must be assigned to a user!");
    public Octokit.GitHubClient AppGithub { get; }
    public OpenAIChatModel Model { get; }
    public Tenant Tenant { get; set; }
    public Account Account { get; set; }
    public Install Install { get; set; }
    public Chat Chat { get; set; }
    public Message Message { get; set; }
    public ITenantService Tenants { get; }
    public IAccountService Accounts { get; }
    public IInstallService Installs { get; }
    public IChatService Chats { get; }
    public IMessageService Messages { get; }
    public ITokenService Tokens { get; }
    public IRecordService Records { get; }
    public IChatDriver Driver { get; }
    public IStorage Storage { get; }
    public CancellationToken CancellationToken { get; }
    public IServiceProvider Services { get; }

    private Message? ProgressMessage { get; set; }
    private IList<(ProgressStyle, string)> ProgressHistory { get; set; } = [];

    public PromptContext(MessageEvent @event, IServiceScope scope, CancellationToken cancellationToken = default)
    {
        AppGithub = scope.ServiceProvider.GetRequiredService<Octokit.GitHubClient>();
        Tenants = scope.ServiceProvider.GetRequiredService<ITenantService>();
        Accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        Installs = scope.ServiceProvider.GetRequiredService<IInstallService>();
        Chats = scope.ServiceProvider.GetRequiredService<IChatService>();
        Messages = scope.ServiceProvider.GetRequiredService<IMessageService>();
        Tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        Model = scope.ServiceProvider.GetRequiredService<OpenAIChatModel>();
        Records = scope.ServiceProvider.GetRequiredService<IRecordService>();
        Driver = scope.ServiceProvider.GetServices<IChatDriver>().First(driver => driver.Type == @event.Message.SourceType);
        Storage = scope.ServiceProvider.GetRequiredService<IStorage>();
        Tenant = @event.Tenant;
        Account = @event.Account;
        Install = @event.Install;
        Chat = @event.Chat;
        Message = @event.Message;
        CancellationToken = cancellationToken;
        Services = scope.ServiceProvider;
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
            Install = Install
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
            Install = Install
        };

        var message = await Driver.Send(request, CancellationToken);
        if (string.IsNullOrEmpty(message.SourceId)) return message;
        return await Storage.Messages.Create(message, cancellationToken: CancellationToken);
    }

    public async Task<Message> Update(Guid id, string text, params Attachment[] attachments)
    {
        var message = await Messages.GetById(id, CancellationToken) ?? throw new Exception("message not found");
        var request = new MessageUpdateRequest()
        {
            Text = text,
            Attachments = attachments.Length > 0 ? attachments : null,
            Chat = Chat,
            From = Account,
            Install = Install,
            Message = message
        };

        message = await Driver.Update(request, CancellationToken);
        if (string.IsNullOrEmpty(message.SourceId)) return message;
        return await Storage.Messages.Update(message, cancellationToken: CancellationToken);
    }

    public async Task<Message> Update(Guid id, params Attachment[] attachments)
    {
        var message = await Messages.GetById(id, CancellationToken) ?? throw new Exception("message not found");
        var request = new MessageUpdateRequest()
        {
            Attachments = attachments.Length > 0 ? attachments : null,
            Chat = Chat,
            From = Account,
            Install = Install,
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
            Install = Install,
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