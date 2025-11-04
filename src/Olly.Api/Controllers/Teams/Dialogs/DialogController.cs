using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;

using Olly.Cards.Dialogs;
using Olly.Errors;
using Olly.Services;

using TaskModules = Microsoft.Teams.Api.TaskModules;

namespace Olly.Api.Controllers.Teams.Dialogs;

[TeamsController]
public partial class DialogController(IHttpContextAccessor accessor)
{
    private IServices Services => accessor.HttpContext!.RequestServices.GetRequiredService<IServices>();

    [TaskFetch]
    public async Task<TaskModules.Response> OnTaskFetch([Context] Tasks.FetchActivity activity, [Context] CancellationToken cancellationToken = default)
    {
        var req = JsonSerializer.Deserialize<OpenDialogRequest>(JsonSerializer.Serialize(activity.Value.Data)) ?? throw new Exception("invalid dialog open request");

        return new TaskModules.Response()
        {
            Task = new TaskModules.ContinueTask(new()
            {
                Title = req.Title,
                Card = new(req.Id switch
                {
                    "chat.jobs" => await OnChatJobsFetch(req, activity, cancellationToken),
                    "message.jobs" => await OnMessageJobsFetch(req, activity, cancellationToken),
                    _ => throw HttpException.BadRequest().AddMessage($"dialog id {req.Id} is not valid")
                })
            })
        };
    }
}