using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

using NetMQ;

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
                        Text = "new message",
                        Data = Data.From(@event.Body)
                    }, lifetime.ApplicationStopping);

                    var context = new PromptContext(@event.Body, scope, lifetime.ApplicationStopping);
                    var mainPrompt = new MainPrompt(context);
                    var prompt = OpenAIChatPrompt.From(model, mainPrompt, new()
                    {
                        Logger = app.Logger
                    });

                    var ok = await OnEvent(context, prompt, lifetime.ApplicationStopping);

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

    private async Task<bool> OnEvent(IPromptContext context, OpenAIChatPrompt prompt, CancellationToken cancellationToken = default)
    {
        await context.Send(new TypingActivity(), cancellationToken);
        var messages = await context.Messages.GetByChatId(context.Chat.Id, cancellationToken: cancellationToken);
        var memory = messages
            .List.Select(m =>
                m.AccountId is null
                    ? new ModelMessage<string>(m.Text) as IMessage
                    : new UserMessage<string>(m.Text)
            )
            .ToList();

        Logger.LogDebug("{}", JsonSerializer.Serialize(memory));

        var res = await prompt.Send(context.Message.Text, new()
        {
            Messages = memory
        }, null, cancellationToken);

        var message = new MessageActivity(res.Content);
        await context.Send(message, cancellationToken);
        return true;
    }
}