using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers;

public class ChatDriverClient()
{
    public Task<Message> Send(string text)
    {
        throw new NotImplementedException();
    }

    public Task<Message> Send(params Attachment[] attachment)
    {
        throw new NotImplementedException();
    }

    public Task<Message> Send(string text, params Attachment[] attachments)
    {
        throw new NotImplementedException();
    }

    public Task<Message> Send(Guid id, string text)
    {
        throw new NotImplementedException();
    }

    public Task<Message> Send(Guid id, params Attachment[] attachment)
    {
        throw new NotImplementedException();
    }

    public Task<Message> Send(Guid id, string text, params Attachment[] attachments)
    {
        throw new NotImplementedException();
    }

    public Task<Message> Reply(string text)
    {
        throw new NotImplementedException();
    }

    public Task<Message> Reply(params Attachment[] attachment)
    {
        throw new NotImplementedException();
    }

    public Task<Message> Reply(string text, params Attachment[] attachments)
    {
        throw new NotImplementedException();
    }
}