using Microsoft.Teams.Api.Activities;

using OS.Agent.Models;

namespace OS.Agent.Drivers;

public interface IDriver
{
    SourceType Type { get; }
}

public interface IChatDriver : IDriver
{
    Task<IActivity> Send(IActivity activity, CancellationToken cancellationToken = default);
}