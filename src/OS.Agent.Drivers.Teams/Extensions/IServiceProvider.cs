using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using OS.Agent.Drivers.Teams.Models;
using OS.Agent.Storage.Postgres;

namespace OS.Agent.Drivers.Teams.Extensions;

public static class IServiceProviderExtensions
{
    public static IServiceCollection AddTeamsDriver(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var jsonOptions = provider.GetRequiredService<JsonSerializerOptions>();

        services.AddScoped<TeamsDriver>();
        services.AddScoped<IDriver>(provider => provider.GetRequiredService<TeamsDriver>());
        services.AddScoped<IChatDriver>(provider => provider.GetRequiredService<TeamsDriver>());

        Dapper.SqlMapper.AddTypeHandler(typeof(TeamsAccountData), new JsonObjectTypeHandler(jsonOptions));
        Dapper.SqlMapper.AddTypeHandler(typeof(TeamsChatData), new JsonObjectTypeHandler(jsonOptions));
        Dapper.SqlMapper.AddTypeHandler(typeof(TeamsMessageData), new JsonObjectTypeHandler(jsonOptions));
        return services;
    }
}