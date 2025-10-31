using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using Olly.Events;
using Olly.Storage.Models;

namespace Olly.Drivers.Teams.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTeamsDriver(this IServiceCollection services)
    {
        ClientRegistry.Register(SourceType.Teams, (@event, provider, cancellationToken) =>
        {
            if (@event is InstallEvent install)
            {
                return new TeamsClient(install, provider, cancellationToken);
            }

            if (@event is MessageEvent message)
            {
                return new TeamsClient(message, provider, cancellationToken);
            }

            throw new InvalidOperationException($"event type '{@event.Key}' is not supported for client type 'Teams'");
        });

        services.AddKeyedSingleton<NetMQQueue<Event>>(SourceType.Teams.ToString());
        services.AddHostedService<TeamsWorker>();
        return services;
    }
}