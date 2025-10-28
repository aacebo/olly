using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Drivers.Teams.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTeamsDriver(this IServiceCollection services)
    {
        ClientRegistry.Register(SourceType.Teams, (@event, provider, cancellationToken) =>
        {
            if (@event is not TeamsEvent teamsEvent)
            {
                throw new InvalidOperationException($"invalid event type '{@event.Key}'");
            }

            return new TeamsClient(teamsEvent, provider, cancellationToken);
        });

        services.AddSingleton<NetMQQueue<TeamsEvent>>();
        services.AddHostedService<TeamsWorker>();
        return services;
    }
}