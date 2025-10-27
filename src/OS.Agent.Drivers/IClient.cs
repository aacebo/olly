using OS.Agent.Cards.Tasks;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers;

public interface IClient
{
    SourceType Type { get; }
}

public interface IAuthClient : IClient
{
    Task SignIn(string url, string state);
}

public interface IChatClient : IClient
{
    Task Typing(string? text = null);
    Task<Message> Send(string text);
    Task<Message> Send(string text, params Attachment[] attachments);
    Task<Message> Send(params Attachment[] attachments);
    Task<Message> Update(Guid id, string? text, params Attachment[] attachments);
    Task<Message> Reply(string text, params Attachment[] attachments);
}

public interface IProgressClient : IChatClient
{
    Task<Message> Progress(string text);
    Task<Message> Progress(string text, params Attachment[] attachments);
    Task<Message> Progress(params Attachment[] attachments);
}

public interface ITaskClient : IProgressClient
{
    Task<TaskItem> Task(TaskItem.Create create);
    Task<TaskItem> Task(Guid id, TaskItem.Update update);
    Task<TaskItem> Finish();
}