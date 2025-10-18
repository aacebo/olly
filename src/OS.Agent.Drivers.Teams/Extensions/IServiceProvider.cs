using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

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

        // Dapper.SqlMapper.AddTypeHandler(typeof(TeamsAccountEntity), new JsonObjectTypeHandler(jsonOptions));
        // Dapper.SqlMapper.AddTypeHandler(typeof(TeamsChatEntity), new JsonObjectTypeHandler(jsonOptions));
        // Dapper.SqlMapper.AddTypeHandler(typeof(TeamsMessageEntity), new JsonObjectTypeHandler(jsonOptions));
        return services;
    }
}