using Microsoft.Teams.Api.Activities;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers;

public interface IDriver
{
    SourceType Type { get; }
}

public interface IChatDriver : IDriver
{
    Task<TActivity> Send<TActivity>(Account account, TActivity activity, CancellationToken cancellationToken = default) where TActivity : IActivity;
    Task<MessageActivity> Reply(Account account, MessageActivity replyTo, MessageActivity message, CancellationToken cancellationToken = default);
}