using OS.Agent.Storage.Extensions;

namespace OS.Agent.Drivers.Teams.Extensions;

public static class IHostExtensions
{
    public static IHost MapTeamsEntityTypes(this IHost host)
    {
        return host.MapEntityTypes();
    }
}