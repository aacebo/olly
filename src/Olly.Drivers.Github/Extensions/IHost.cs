using Microsoft.Extensions.Hosting;

using Olly.Storage.Extensions;

namespace Olly.Drivers.Github.Extensions;

public static class IHostExtensions
{
    public static IHost MapGithubEntityTypes(this IHost host)
    {
        return host.MapEntityTypes();
    }
}