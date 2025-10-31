using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Messages;

using Olly.Drivers.Github.Prompts;
using Olly.Events;
using Olly.Prompts;
using Olly.Prompts.Extensions;
using Olly.Storage;

namespace Olly.Drivers.Teams;

public partial class TeamsWorker
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
        var prompt = OllyPrompt.Create(client, provider, cancellationToken)
            .AddPrompt(GithubPrompt.Create(client, provider, cancellationToken), cancellationToken);

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

    protected Task OnMessageUpdateEvent(MessageEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageDeleteEvent(MessageEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected async Task OnMessageResumeEvent(MessageEvent @event, Client client, CancellationToken cancellationToken = default)
    {
        var prompt = OllyPrompt.Create(client, provider, cancellationToken)
            .AddPrompt(GithubPrompt.Create(client, provider, cancellationToken), cancellationToken);

        await client.Typing();

        var messages = await client.Services.Messages.GetByChatId(
            client.Chat.Id,
            Page.Create().Sort(SortDirection.Asc, "created_at").Build(),
            client.CancellationToken
        );

        var memory = messages.List
            .Select(m =>
                m.AccountId is null
                    ? new ModelMessage<string>(m.Text) as IMessage
                    : new UserMessage<string>(m.Text)
            )
            .ToList();

        var res = await prompt.Send($@"Resume from ""{@event.Message.Text}""", new()
        {
            Messages = memory,
            Request = new()
            {
                Temperature = 0,
                EndUserId = client.User?.Id.ToString()
            }
        }, null, cancellationToken);

        await client.Finish();
        await client.SendReply(res.Content);
    }
}