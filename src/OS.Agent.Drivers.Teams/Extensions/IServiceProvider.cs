using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams.Extensions;

public static class IServiceProviderExtensions
{
    public static IServiceCollection AddTeamsDriver(this IServiceCollection services)
    {
        services.AddKeyedScoped<TeamsDriver>(SourceType.Teams.ToString());
        services.AddKeyedScoped<IDriver, TeamsDriver>(SourceType.Teams.ToString());
        services.AddKeyedScoped<IChatDriver, TeamsDriver>(SourceType.Teams.ToString());
        return services;
    }
}