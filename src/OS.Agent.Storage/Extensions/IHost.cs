using System.Reflection;

using Microsoft.Extensions.Hosting;

using OS.Agent.Storage.Models;

namespace OS.Agent.Storage.Extensions;

public static class IHostExtensions
{
    public static IHost MapEntityTypes(this IHost host)
    {
        foreach (var type in Assembly.GetCallingAssembly().GetTypes())
        {
            foreach (var attribute in type.GetCustomAttributes<EntityAttribute>())
            {
                EntityTypeRegistry.Types.Add(attribute.Name, type);
            }
        }

        return host;
    }
}