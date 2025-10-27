using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using OS.Agent.Drivers.Teams.Events;

namespace OS.Agent.Drivers.Teams.Extensions;

public static class IServiceProviderExtensions
{
    public static IServiceCollection AddTeamsDriver(this IServiceCollection services)
    {
        services.AddSingleton<NetMQQueue<TeamsEvent>>();
        services.AddHostedService<TeamsWorker>();
        return services;
    }
}