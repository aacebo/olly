using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

using NetMQ;

using OS.Agent.Models;
using OS.Agent.Prompts;
using OS.Agent.Stores;

namespace OS.Agent.Workers;

public class MessageActivityWorker(ILogger<MessageActivityWorker> logger, IWebHostEnvironment env, App app, OpenAIChatModel model, NetMQQueue<Event<MessageActivity>> events, IServiceScopeFactory scopeFactory) : IHostedService
{
    private readonly NetMQPoller _poller = [events];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("starting...");
        events.ReceiveReady += async (_, args) =>
        {
            var scope = scopeFactory.CreateScope();
            var lifetime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
            var storage = scope.ServiceProvider.GetRequiredService<IStorage>();

            while (args.Queue.TryDequeue(out var @event, TimeSpan.FromMilliseconds(200)))
            {
                try
                {
                    var activity = @event.Body;
                    logger.LogDebug("{}", activity);

                    var tenant = await storage.Tenants.GetBySourceId(
                        SourceType.Teams,
                        activity.Conversation.TenantId!,
                        cancellationToken
                    );

                    if (tenant is null)
                    {
                        throw new Exception("UnAuthorized: Tenant not found");
                    }

                    var account = await storage.Accounts.GetBySourceId(
                        tenant.Id,
                        SourceType.Teams,
                        activity.From.Id,
                        cancellationToken
                    );

                    if (account is null)
                    {
                        throw new Exception("UnAuthorized: Account not found");
                    }

                    var chat = await storage.Chats.GetBySourceId(
                        tenant.Id,
                        SourceType.Teams,
                        activity.Conversation.Id,
                        cancellationToken
                    );

                    if (chat is null)
                    {
                        throw new Exception("UnAuthorized: Chat not found");
                    }

                    var context = new PromptContext(activity, scope, lifetime.ApplicationStopping)
                    {
                        Account = account,
                        Chat = chat,
                        Tenant = tenant
                    };

                    var mainPrompt = new MainPrompt(context);
                    var prompt = OpenAIChatPrompt.From(model, mainPrompt, new()
                    {
                        Logger = app.Logger
                    });

                    var ok = await OnEvent(context, prompt, lifetime.ApplicationStopping);

                    if (!ok)
                    {
                        logger.LogWarning("invalid event");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("{}", ex);
                    throw new Exception("MessageActivityWorker", ex);
                }
            }
        };

        _poller.RunAsync();
        logger.LogInformation("listening...");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("stopping...");
        _poller.StopAsync();
        logger.LogInformation("stopped");
        return Task.CompletedTask;
    }

    private async Task<bool> OnEvent(IPromptContext context, OpenAIChatPrompt prompt, CancellationToken cancellationToken = default)
    {
        await context.Send(new TypingActivity());

        var now = DateTime.UtcNow;
        var res = await prompt.Send(context.Activity.Text, null, cancellationToken);
        var elapse = DateTime.UtcNow - now;
        var message = new MessageActivity(res.Content);

        if (env.IsDevelopment() || env.IsEnvironment("Local"))
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