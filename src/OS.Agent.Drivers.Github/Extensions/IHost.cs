using OS.Agent.Storage.Extensions;

namespace OS.Agent.Drivers.Github.Extensions;

public static class IHostExtensions
{
    public static IHost MapGithubEntityTypes(this IHost host)
    {
        return host.MapEntityTypes();
    }
}