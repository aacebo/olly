using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsClient
{
    public override async Task<Message> SendProgress(string text)
    {
        if (Response.Progress is null)
        {
            Response.Progress = await Send(text);
            return Response.Progress;
        }

        Response.Progress = await SendUpdate(Response.Progress.Id, text);
        return Response.Progress;
    }

    public override async Task<Message> SendProgress(params Attachment[] attachments)
    {
        if (Response.Progress is null)
        {
            Response.Progress = await Send("please wait...");
        }

        Response.Progress = await SendUpdate(Response.Progress.Id, null, attachments);
        return Response.Progress;
    }

    public override async Task<Message> SendProgress(string text, params Attachment[] attachments)
    {
        if (Response.Progress is null)
        {
            Response.Progress = await Send(text);
        }

        Response.Progress = await SendUpdate(Response.Progress.Id, null, attachments);
        return Response.Progress;
    }
}