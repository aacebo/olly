using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Models;
using OS.Agent.Prompts;
using OS.Agent.Services;
using OS.Agent.Stores;

namespace OS.Agent.Workers;

public class MessageWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private ILogger<MessageWorker> Logger { get; init; } = provider.GetRequiredService<ILogger<MessageWorker>>();
    private NetMQQueue<Event<MessageEvent>> Events { get; init; } = provider.GetRequiredService<NetMQQueue<Event<MessageEvent>>>();
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
                    Logger.LogDebug("{}", @event);
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
        var res = await prompt.Send(context.Message.Text, null, cancellationToken);
        var message = new MessageActivity(res.Content);
        await context.Send(message, cancellationToken);
        return true;
    }
}