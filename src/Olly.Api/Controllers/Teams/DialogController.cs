using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Cards;

using TaskModules = Microsoft.Teams.Api.TaskModules;

namespace Olly.Api.Controllers.Teams;

[TeamsController]
public class DialogController(IHttpContextAccessor _)
{
    [TaskFetch]
    public async Task<TaskModules.Response> OnTaskFetch([Context] Tasks.FetchActivity activity, [Context] CancellationToken cancellationToken = default)
    {
        return new TaskModules.Response()
        {
            Task = new TaskModules.ContinueTask(new()
            {
                Title = "Hello World",
                Card = new(new Microsoft.Teams.Cards.AdaptiveCard(
                    new TextBlock("hi!")
                ))
            })
        };
    }
}