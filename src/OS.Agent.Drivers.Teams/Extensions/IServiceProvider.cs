using NetMQ;

using OS.Agent.Drivers.Teams.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Extensions;

public static class IServiceProviderExtensions
{
    public static IServiceCollection AddTeamsDriver(this IServiceCollection services)
    {
        services.AddKeyedScoped<TeamsDriver>(SourceType.Teams.ToString());
        services.AddKeyedScoped<IDriver, TeamsDriver>(SourceType.Teams.ToString());
        services.AddKeyedScoped<IChatDriver, TeamsDriver>(SourceType.Teams.ToString());
        services.AddSingleton<NetMQQueue<TeamsEvent>>();
        services.AddHostedService<TeamsWorker>();
        return services;
    }
}