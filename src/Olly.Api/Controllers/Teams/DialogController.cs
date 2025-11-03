using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Cards;

using Olly.Cards.Dialogs;
using Olly.Services;

using TaskModules = Microsoft.Teams.Api.TaskModules;

namespace Olly.Api.Controllers.Teams;

[TeamsController]
public class DialogController(IHttpContextAccessor accessor)
{
    [TaskFetch]
    public TaskModules.Response OnTaskFetch([Context] Tasks.FetchActivity activity, [Context] CancellationToken cancellationToken = default)
    {
        var _ = accessor.HttpContext!.RequestServices.GetRequiredService<IServices>();
        var request = JsonSerializer.Deserialize<OpenDialogRequest>(JsonSerializer.Serialize(activity.Value.Data)) ?? throw new Exception("invalid dialog open request");

        return new TaskModules.Response()
        {
            Task = new TaskModules.ContinueTask(new()
            {
                Title = request.Title,
                Card = new(new Microsoft.Teams.Cards.AdaptiveCard(
                    new TextBlock("hi!")
                ))
            })
        };
    }
}