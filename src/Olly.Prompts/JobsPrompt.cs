using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using Olly.Cards.Progress;
using Olly.Drivers;
using Olly.Storage;
using Olly.Storage.Models;

namespace Olly.Prompts;

[Prompt("JobsAgent")]
[Prompt.Description(
    "An agent that can get/create/update jobs.",
    "A Job in Olly's database represents a unit of work or task that has a start time, end time, and status.",
    "If a job is of type sync, it is synchronous, meaning it blocks the agent from continuing until it is completed.",
    "If a job is of type async, it is asynchronous, meaning it is not blocking and the agent can continue while it runs in the background."
)]
[Prompt.Instructions(
    "<agent>",
        "You are an agent that is an expert at fetching/creating/updating Jobs.",
        "A Job represents a unit of work or task that has a start time, end time, and status.",
        "If a job is of type sync, it is synchronous, meaning it blocks the agent from continuing until it is completed.",
        "If a job is of type async, it is asynchronous, meaning it is not blocking and the agent can continue while it runs in the background.",
    "</agent>"
)]
public class JobsPrompt
{
    private Client Client { get; }

    public static OpenAIChatPrompt Create(Client client, IServiceProvider provider)
    {
        var model = provider.GetRequiredService<OpenAIChatModel>();
        var logger = provider.GetRequiredService<Microsoft.Teams.Common.Logging.ILogger>();

        return OpenAIChatPrompt.From(model, new JobsPrompt(client), new()
        {
            Logger = logger
        });
    }

    public JobsPrompt(Client client)
    {
        Client = client;
    }

    [Function]
    [Function.Description("Get the list of running jobs")]
    public async Task<string> GetRunning()
    {
        var res = await Client.Services.Jobs.GetByChatId(
            Client.Chat.Id,
            Page.Create()
                .Size(25)
                .Sort(SortDirection.Desc, "created_at")
                .Factory(q => q.WhereNull("ended_at"))
                .Build(),
            Client.CancellationToken
        );

        return JsonSerializer.Serialize(new
        {
            count = res.Count,
            page_count = res.TotalPages,
            page = res.Page,
            page_size = res.PerPage,
            data = res.List
        }, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("Create a new job and start it")]
    public async Task<string> Start([Param] string name, [Param] string title, [Param] string message)
    {
        var job = await Client.Services.Jobs.Create(new()
        {
            InstallId = Client.Install.Id,
            ChatId = Client.Chat.Id,
            MessageId = Client.Message?.Id,
            Name = name,
            Status = JobStatus.Running,
            Title = title,
            Message = message
        }, Client.CancellationToken);

        await Client.SendTask(new()
        {
            Id = job.Id,
            Title = job.Title,
            Style = ProgressStyle.InProgress,
            Message = message
        });

        return JsonSerializer.Serialize(job, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("End a running job successfully")]
    public async Task<string> EndSuccess([Param] Guid jobId)
    {
        var job = await Client.Services.Jobs.GetById(jobId) ?? throw new Exception("job not found");
        job = await Client.Services.Jobs.Update(job.Success(), Client.CancellationToken);

        await Client.SendTask(job.Id, new()
        {
            Title = job.Title,
            Style = ProgressStyle.Success,
            Message = job.Message,
            EndedAt = job.EndedAt
        });

        return JsonSerializer.Serialize(job, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("End a running job with an error")]
    public async Task<string> EndError([Param] Guid jobId, [Param] string message)
    {
        var job = await Client.Services.Jobs.GetById(jobId) ?? throw new Exception("job not found");
        job = await Client.Services.Jobs.Update(job.Error(message), Client.CancellationToken);

        await Client.SendTask(job.Id, new()
        {
            Title = job.Title,
            Style = ProgressStyle.Error,
            Message = job.Message,
            EndedAt = job.EndedAt
        });

        return JsonSerializer.Serialize(job, Client.JsonSerializerOptions);
    }
}