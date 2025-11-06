using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;

using Olly.Cards.Progress;
using Olly.Drivers;
using Olly.Storage;

namespace Olly.Prompts;

[Prompt("JobsAgent")]
[Prompt.Description(
    "An agent that can get/create/update jobs.",
    "A Job is a way to communicate to the user what you are working on.",
    "Jobs do not do anything themselves, they should be used to send informative updates to the user only!",
    "All jobs that are started must also be ended (success/warning/error) when you are done the job/task.",
    "NEVER LEAVE A JOB RUNNING!"
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
                .Sort(SortDirection.Desc, "jobs.created_at")
                .Factory(q => q
                    .LeftJoin("job_runs", "jobs.last_run_id", "job_runs.id")
                    .WhereNull("job_runs.ended_at")
                )
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
    public async Task<string> CreateAndStart([Param] string name, [Param] string title, [Param] string description)
    {
        var job = await Client.Services.Jobs.Create(new()
        {
            InstallId = Client.Install.Id,
            ChatId = Client.Chat.Id,
            MessageId = Client.Message?.Id,
            Name = name,
            Title = title,
            Description = description
        }, Client.CancellationToken);

        var run = await Client.Services.Runs.Create(new()
        {
            JobId = job.Id,
            StartedAt = DateTimeOffset.UtcNow
        }, Client.CancellationToken);

        await Client.SendTask(new()
        {
            Id = run.Id,
            Title = job.Title,
            Style = ProgressStyle.InProgress,
            Message = description
        });

        return JsonSerializer.Serialize(run, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("End a running job successfully")]
    public async Task<string> UpdateAsSuccess([Param] Guid id)
    {
        var run = await Client.Services.Runs.GetById(id) ?? throw new Exception("job run not found");
        var job = await Client.Services.Jobs.GetById(run.JobId) ?? throw new Exception("job not found");
        run = await Client.Services.Runs.Update(run.Success(), Client.CancellationToken);

        await Client.SendTask(run.Id, new()
        {
            Title = job.Title,
            Style = ProgressStyle.Success,
            Message = run.StatusMessage,
            EndedAt = run.EndedAt
        });

        return JsonSerializer.Serialize(run, Client.JsonSerializerOptions);
    }

    [Function]
    [Function.Description("End a running job with an error")]
    public async Task<string> UpdateAsError([Param] Guid id, [Param] string message)
    {
        var run = await Client.Services.Runs.GetById(id) ?? throw new Exception("job run not found");
        var job = await Client.Services.Jobs.GetById(run.JobId) ?? throw new Exception("job not found");
        run = await Client.Services.Runs.Update(run.Error(message), Client.CancellationToken);

        await Client.SendTask(job.Id, new()
        {
            Title = job.Title,
            Style = ProgressStyle.Error,
            Message = run.StatusMessage,
            EndedAt = run.EndedAt
        });

        return JsonSerializer.Serialize(run, Client.JsonSerializerOptions);
    }
}