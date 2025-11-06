using Microsoft.Extensions.DependencyInjection;

namespace Olly.Contexts.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddOllyContextAccessor(this IServiceCollection services)
    {
        return services
            .AddScoped<OllyContext>()
            .AddSingleton<IOllyContextAccessor, OllyContextAccessor>();
    }
}