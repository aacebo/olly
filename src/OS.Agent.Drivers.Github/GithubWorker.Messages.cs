using Microsoft.Extensions.DependencyInjection;

using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models.OpenAI;

using OS.Agent.Events;
using OS.Agent.Prompts;
using OS.Agent.Prompts.Extensions;
using OS.Agent.Storage;

namespace OS.Agent.Drivers.Github;

public partial class GithubWorker
{
    protected async Task OnMessageEvent(MessageEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        if (@event.Action.IsCreate)
        {
            await OnMessageCreateEvent(@event, client, cancellationToken);
            return;
        }
        else if (@event.Action.IsUpdate)
        {
            await OnMessageUpdateEvent(@event, client, cancellationToken);
            return;
        }
        else if (@event.Action.IsDelete)
        {
            await OnMessageDeleteEvent(@event, client, cancellationToken);
            return;
        }
        else if (@event.Action.IsResume)
        {
            await OnMessageResumeEvent(@event, client, cancellationToken);
            return;
        }

        throw new Exception($"event '{@event.Key}' not found");
    }

    protected async Task OnMessageCreateEvent(MessageEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        var model = client.Provider.GetRequiredService<OpenAIChatModel>();
        var logger = client.Provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>();
        var githubPrompt = OpenAIChatPrompt.From(model, new GithubPrompt(client), new()
        {
            Logger = logger
        });

        var recordsPrompt = OpenAIChatPrompt.From(model, new RecordsPrompt(client), new()
        {
            Logger = logger
        });

        var prompt = OpenAIChatPrompt.From(model, new OllyPrompt(client), new()
        {
            Logger = logger
        });

        prompt = prompt
            .AddPrompt(githubPrompt, client.CancellationToken)
            .AddPrompt(recordsPrompt, client.CancellationToken);

        await client.Typing();

        var messages = await client.Services.Messages.GetByChatId(
            client.Chat.Id,
            Page.Create().Sort(SortDirection.Desc, "created_at").Build(),
            client.CancellationToken
        );

        var memory = messages.List
            .Select(m =>
                m.AccountId is null
                    ? new ModelMessage<string>(m.Text) as IMessage
                    : new UserMessage<string>(m.Text)
            )
            .ToList();

        var res = await prompt.Send(@event.Message.Text, new()
        {
            Messages = memory,
            Request = new()
            {
                Temperature = 0,
                EndUserId = client.User?.Id.ToString()
            }
        }, null, cancellationToken);

        await client.Finish();
        await client.Send(res.Content);
    }

    protected Task OnMessageUpdateEvent(MessageEvent @event, Client client, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageDeleteEvent(MessageEvent @event, Client client, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageResumeEvent(MessageEvent @event, Client client, CancellationToken _ = default)
    {
        return Task.CompletedTask;
    }
}