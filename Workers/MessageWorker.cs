using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

using NetMQ;

using OS.Agent.Events;
using OS.Agent.Models;
using OS.Agent.Prompts;
using OS.Agent.Stores;

namespace OS.Agent.Workers;

public class MessageWorker(IServiceProvider provider, IServiceScopeFactory scopeFactory) : IHostedService
{
    private IWebHostEnvironment Env { get; init; } = provider.GetRequiredService<IWebHostEnvironment>();
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

            while (args.Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
            {
                try
                {
                    Logger.LogDebug("{}", @event);

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
                    throw new Exception("MessageActivityWorker", ex);
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

        var now = DateTime.UtcNow;
        var res = await prompt.Send(context.Message.Text, null, cancellationToken);
        var elapse = DateTime.UtcNow - now;
        var message = new MessageActivity(res.Content);

        if (Env.IsDevelopment() || Env.IsEnvironment("Local"))
        {
            message = message.AddAttachment(new AdaptiveCard(
                new ColumnSet().WithColumns(
                    new Column(
                        new TextBlock("Elapse Time:")
                            .WithWeight(TextWeight.Bolder)
                            .WithFontType(FontType.Monospace)
                            .WithIsSubtle(true)
                            .WithWrap(false)
                    ).WithWidth(new Union<string, float>("auto")),
                    new Column(
                        new TextBlock($"{elapse.Milliseconds}ms")
                            .WithIsSubtle(true)
                            .WithWrap(false)
                            .WithHorizontalAlignment(HorizontalAlignment.Left)
                    )
                    .WithHorizontalAlignment(HorizontalAlignment.Left)
                    .WithWidth(new Union<string, float>("stretch"))
                )
            ));
        }

        await context.Send(message);
        return true;
    }
}