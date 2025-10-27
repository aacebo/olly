using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsClient
{
    public async Task<Message> Progress(string text)
    {
        if (Response.Progress is null)
        {
            Response.Progress = await Send(text);
            return Response.Progress;
        }

        Response.Progress = await Update(Response.Progress.Id, text);
        return Response.Progress;
    }

    public async Task<Message> Progress(params Attachment[] attachments)
    {
        if (Response.Progress is null)
        {
            Response.Progress = await Send("please wait...");
        }

        Response.Progress = await Update(Response.Progress.Id, null, attachments);
        return Response.Progress;
    }

    public async Task<Message> Progress(string text, params Attachment[] attachments)
    {
        if (Response.Progress is null)
        {
            Response.Progress = await Send(text);
        }

        Response.Progress = await Update(Response.Progress.Id, null, attachments);
        return Response.Progress;
    }
}