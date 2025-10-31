using System.Reflection;

using Microsoft.Extensions.Hosting;

using Olly.Storage.Models;

namespace Olly.Storage.Extensions;

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