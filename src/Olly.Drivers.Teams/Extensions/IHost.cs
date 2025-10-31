using Microsoft.Extensions.Hosting;

using Olly.Storage.Extensions;

namespace Olly.Drivers.Teams.Extensions;

public static class IHostExtensions
{
    public static IHost MapTeamsEntityTypes(this IHost host)
    {
        return host.MapEntityTypes();
    }
}