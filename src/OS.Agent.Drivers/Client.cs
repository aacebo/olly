using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OS.Agent.Cards.Tasks;
using OS.Agent.Services;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers;

/// <summary>
/// The base for all driver client implementations
/// </summary>
public abstract class Client
{
    public IServiceProvider Provider { get; }
    public IServices Services { get; }
    public IStorage Storage { get; }
    public JsonSerializerOptions JsonSerializerOptions { get; }
    public CancellationToken CancellationToken { get; }
    public IReadOnlyList<TaskItem> Tasks => Response.TaskCard.Tasks.ToList();

    public abstract Tenant Tenant { get; }
    public abstract Account Account { get; }
    public abstract User User { get; }
    public abstract Chat Chat { get; }
    public abstract Message Message { get; }

    protected virtual ILogger<Client> Logger { get; }
    protected ClientResponse Response { get; } = new();

    public Client(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        Provider = provider;
        Services = provider.GetRequiredService<IServices>();
        Storage = provider.GetRequiredService<IStorage>();
        JsonSerializerOptions = provider.GetRequiredService<JsonSerializerOptions>();
        Logger = provider.GetRequiredService<ILogger<Client>>();
        CancellationToken = cancellationToken;
    }

    public virtual Task SignIn(string url, string state)
    {
        return Task.CompletedTask;
    }

    public virtual Task Typing(string? text = null)
    {
        return Task.CompletedTask;
    }

    public virtual Task<Message> Send(string text)
    {
        throw new NotImplementedException();
    }

    public virtual Task<Message> Send(string text, params Attachment[] attachments)
    {
        throw new NotImplementedException();
    }

    public virtual Task<Message> Send(params Attachment[] attachments)
    {
        throw new NotImplementedException();
    }

    public virtual Task<Message> SendUpdate(Guid id, string? text, params Attachment[] attachments)
    {
        throw new NotImplementedException();
    }

    public virtual Task<Message> SendReply(string text, params Attachment[] attachments)
    {
        throw new NotImplementedException();
    }

    public virtual Task<Message> SendProgress(string text)
    {
        throw new NotImplementedException();
    }

    public virtual Task<Message> SendProgress(string text, params Attachment[] attachments)
    {
        throw new NotImplementedException();
    }

    public virtual Task<Message> SendProgress(params Attachment[] attachments)
    {
        throw new NotImplementedException();
    }

    public virtual Task<TaskItem> SendTask(TaskItem.Create create)
    {
        throw new NotImplementedException();
    }

    public virtual Task<TaskItem> SendTask(Guid id, TaskItem.Update update)
    {
        throw new NotImplementedException();
    }

    public virtual Task<TaskItem> Finish()
    {
        throw new NotImplementedException();
    }
}