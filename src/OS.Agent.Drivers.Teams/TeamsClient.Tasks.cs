using System.Text.Json;

using OS.Agent.Cards.Progress;
using OS.Agent.Cards.Tasks;
using OS.Agent.Drivers.Teams.Extensions;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsClient
{
    public async Task<TaskItem> Task(TaskItem.Create create)
    {
        var task = Response.TaskCard.Add(create);
        var attachment = Response.TaskCard.Render().ToAttachment();

        await Progress(new Attachment()
        {
            ContentType = attachment.ContentType,
            Content = attachment.Content ?? throw new JsonException()
        });

        await Typing();
        return task;
    }

    public async Task<TaskItem> Task(Guid id, TaskItem.Update update)
    {
        var task = Response.TaskCard.Update(id, update);
        var attachment = Response.TaskCard.Render().ToAttachment();

        await Progress(new Attachment()
        {
            ContentType = attachment.ContentType,
            Content = attachment.Content ?? throw new JsonException()
        });

        await Typing();
        return task;
    }

    public async Task<TaskItem> Finish()
    {
        var errors = Response.TaskCard.Tasks.Count(t => t.Style.IsError);
        var warnings = Response.TaskCard.Tasks.Count(t => t.Style.IsWarning);
        var task = Response.TaskCard.Current is not null
            ? Response.TaskCard.Current
            : Response.TaskCard.Add(new()
            {
                Style = ProgressStyle.InProgress,
                Title = "✅ Done!",
                Message = "Success!"
            });

        task = Response.TaskCard.Update(task.Id, new()
        {
            EndedAt = DateTimeOffset.UtcNow
        });

        var attachment = Response.TaskCard.Render().ToAttachment();

        await Progress(new Attachment()
        {
            ContentType = attachment.ContentType,
            Content = attachment.Content ?? throw new JsonException()
        });

        await Typing();
        return task;
    }
}