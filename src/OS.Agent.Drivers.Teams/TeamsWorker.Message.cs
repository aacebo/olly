using Json.Schema;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models.OpenAI;

using OS.Agent.Drivers.Github;
using OS.Agent.Drivers.Teams.Events;
using OS.Agent.Prompts;
using OS.Agent.Storage;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsWorker
{
    protected async Task OnMessageEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
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

    protected async Task OnMessageCreateEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        var model = client.Provider.GetRequiredService<OpenAIChatModel>();
        var githubPrompt = OpenAIChatPrompt.From(model, new GithubPrompt(client), new()
        {
            Logger = client.Provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>()
        });
        
        var prompt = OpenAIChatPrompt.From(model, new MainPrompt(client), new()
        {
            Logger = client.Provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>()
        });

        prompt.Function(
            "Github",
            githubPrompt.Description,
            new JsonSchemaBuilder().Type(SchemaValueType.Object).Properties(
                ("message", new JsonSchemaBuilder().Type(SchemaValueType.String).Description("message to send"))
            ),
            async (string message) =>
            {
                var res = await githubPrompt.Send(message, new()
                {
                    Request = new()
                    {
                        Temperature = 0,
                        EndUserId = client.User.Id.ToString()
                    }
                }, null, cancellationToken);
                return res.Content;
            }
        );

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
                EndUserId = client.User.Id.ToString()
            }
        }, null, cancellationToken);

        await client.Finish();
        await client.Send(res.Content);
    }

    protected Task OnMessageUpdateEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected Task OnMessageDeleteEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected async Task OnMessageResumeEvent(TeamsMessageEvent @event, TeamsClient client, CancellationToken cancellationToken = default)
    {
        var model = client.Provider.GetRequiredService<OpenAIChatModel>();
        var githubPrompt = OpenAIChatPrompt.From(model, new GithubPrompt(client), new()
        {
            Logger = client.Provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>()
        });
        
        var prompt = OpenAIChatPrompt.From(model, new MainPrompt(client), new()
        {
            Logger = client.Provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>()
        });

        prompt.Function(
            "Github",
            githubPrompt.Description,
            new JsonSchemaBuilder().Type(SchemaValueType.Object).Properties(
                ("message", new JsonSchemaBuilder().Type(SchemaValueType.String).Description("message to send"))
            ),
            async (string message) =>
            {
                var res = await githubPrompt.Send(message, new()
                {
                    Request = new()
                    {
                        Temperature = 0,
                        EndUserId = client.User.Id.ToString()
                    }
                }, null, cancellationToken);
                return res.Content;
            }
        );

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

        var res = await prompt.Send($@"Resume from ""{@event.Message.Text}""", new()
        {
            Messages = memory,
            Request = new()
            {
                Temperature = 0,
                EndUserId = client.User.Id.ToString()
            }
        }, null, cancellationToken);

        await client.Finish();
        await client.SendReply(res.Content);
    }
}