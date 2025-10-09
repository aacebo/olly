using System.Reflection;
using System.Text.Json.Serialization;

namespace OS.Agent.Extensions;

public static class ObjectExtensions
{
    public static IDictionary<string, object?> ToDictionary(this object value)
    {
        var type = value.GetType();
        var data = new Dictionary<string, object?>();

        foreach (var field in type.GetFields())
        {
            var name = field.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? field.Name;
            data[name] = field.GetValue(value);
        }

        return data;
    }
}