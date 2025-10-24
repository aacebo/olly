using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Apps;

using NetMQ;

using OS.Agent.Cards.Progress;
using OS.Agent.Contexts;
using OS.Agent.Events;
using OS.Agent.Prompts;
using OS.Agent.Services;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Workers;

public class MessageWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private ILogger<MessageWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<MessageWorker>>();
    private NetMQQueue<Event<MessageEvent>> Events { get; init; } = provider.GetRequiredService<NetMQQueue<Event<MessageEvent>>>();
    private JsonSerializerOptions JsonOptions { get; init; } = provider.GetRequiredService<JsonSerializerOptions>();
    private NetMQPoller Poller { get; init; } = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("starting...");
        Poller.Add(Events);
        Events.ReceiveReady += async (_, args) =>
        {
            var scope = scopeFactory.CreateScope();
            var lifetime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
            var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
            var app = scope.ServiceProvider.GetRequiredService<App>();
            var model = scope.ServiceProvider.GetRequiredService<OpenAIChatModel>();
            var logs = scope.ServiceProvider.GetRequiredService<ILogService>();

            while (args.Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
            {
                try
                {
                    Logger.LogDebug("{}", JsonSerializer.Serialize(@event, JsonOptions));

                    await logs.Create(new()
                    {
                        TenantId = @event.Body.Tenant.Id,
                        Type = LogType.Message,
                        TypeId = @event.Body.Message.Id.ToString(),
                        Text = @event.Name,
                        Entities = [Entity.From(@event.Body)]
                    }, lifetime.ApplicationStopping);

                    if (@event.Body.Account.UserId is null) continue;

                    var user = await storage.Users.GetById(@event.Body.Account.UserId.Value, lifetime.ApplicationStopping);

                    if (user is null) continue;

                    var context = new AgentMessageContext(@event.Body.Account.SourceType, scope, lifetime.ApplicationStopping)
                    {
                        Tenant = @event.Body.Tenant,
                        Account = @event.Body.Account,
                        User = user,
                        Chat = @event.Body.Chat,
                        Message = @event.Body.Message,
                        Installation = @event.Body.Install
                    };

                    var mainPrompt = new MainPrompt(context);
                    var prompt = OpenAIChatPrompt.From(model, mainPrompt, new()
                    {
                        Logger = app.Logger
                    });

                    var ok = @event.Name switch
                    {
                        "messages.create" => await OnCreateEvent(context, prompt),
                        "messages.resume" => await OnResumeEvent(context, prompt),
                        _ => throw new NotImplementedException($"Event '{@event.Name}' is not implemented")
                    };

                    if (!ok)
                    {
                        Logger.LogWarning("invalid event");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("{}", ex);
                    throw new Exception("MessageWorker", ex);
                }
            }
        };

        Poller.RunAsync();
        Logger.LogInformation("listening...");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("stopping...");
        Poller.StopAsync();
        Logger.LogInformation("stopped");
        return Task.CompletedTask;
    }

    private async Task<bool> OnCreateEvent(AgentMessageContext context, OpenAIChatPrompt prompt)
    {
        await context.Typing();

        var messages = await context.Services.Messages.GetByChatId(
            context.Chat.Id,
            Page.Create().Sort(SortDirection.Desc, "created_at").Build(),
            context.CancellationToken
        );

        var memory = messages.List
            .Select(m =>
                m.AccountId is null
                    ? new ModelMessage<string>(m.Text) as IMessage
                    : new UserMessage<string>(m.Text)
            )
            .ToList();

        var res = await prompt.Send(context.Message.Text, new()
        {
            Messages = memory,
            Request = new()
            {
                Temperature = 0,
                EndUserId = context.User.Id.ToString()
            }
        }, null, context.CancellationToken);

        await context.SendProgressUpdate(ProgressStyle.Success, "✅ Done!");
        await context.Send(res.Content);
        return true;
    }

    private async Task<bool> OnResumeEvent(AgentMessageContext context, OpenAIChatPrompt prompt)
    {
        await context.Typing();

        var messages = await context.Services.Messages.GetByChatId(
            context.Chat.Id,
            Page.Create().Sort(SortDirection.Desc, "created_at").Build(),
            context.CancellationToken
        );

        var memory = messages.List
            .Select(m =>
                m.AccountId is null
                    ? new ModelMessage<string>(m.Text) as IMessage
                    : new UserMessage<string>(m.Text)
            )
            .ToList();

        var res = await prompt.Send($"Resume from ", new()
        {
            Messages = memory
        }, null, context.CancellationToken);

        await context.SendProgressUpdate(ProgressStyle.Success, "✅ Done!");
        await context.Reply(res.Content);
        return true;
    }
}