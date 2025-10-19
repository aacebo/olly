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
            var attribute = type.GetCustomAttribute<EntityAttribute>();
            if (attribute is null) continue;
            EntityTypeRegistry.Types.Add(attribute.Name, type);
        }

        return host;
    }
}