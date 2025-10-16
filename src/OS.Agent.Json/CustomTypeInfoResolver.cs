using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using OS.Agent.Json.Extensions;

namespace OS.Agent.Json;

public class CustomTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var info = base.GetTypeInfo(type, options);
        var attributes = info.Type.GetCustomAttributes<JsonDerivedFromTypeAttribute>();

        foreach (var attribute in attributes)
        {
            var derivedFromType = base.GetTypeInfo(attribute.From, options);

            if (derivedFromType.PolymorphismOptions is not null)
            {
                derivedFromType.AddUniqueDerivedType(info.Type, attribute.Descriminator);
                Console.WriteLine($"{derivedFromType.Type} => [{string.Join(",", derivedFromType.PolymorphismOptions.DerivedTypes.Select(d => d.DerivedType.ToString()))}]");
            }
        }

        var derivedToTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => info.Type.IsAssignableFrom(t));

        foreach (var derivedToType in derivedToTypes)
        {
            foreach (var attribute in derivedToType.GetCustomAttributes<JsonDerivedFromTypeAttribute>().Where(a => a.From == type))
            {
                info.AddUniqueDerivedType(derivedToType, attribute.Descriminator);
            }
        }

        return info;
    }
}