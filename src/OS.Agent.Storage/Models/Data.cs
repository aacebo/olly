using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.More;

namespace OS.Agent.Storage.Models;

[JsonPolymorphic]
[JsonDerivedType(typeof(Data), "data")]
public class Data
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();

    public JsonElement? Get(string path)
    {
        var parts = path.Split('.', 1);
        JsonElement value = Properties.ToJsonDocument().RootElement;

        foreach (var part in parts)
        {
            if (!value.TryGetProperty(part, out var el))
            {
                return default;
            }

            value = el;
        }

        return value;
    }

    public static Data From<T>(T value, JsonSerializerOptions? options = null) where T : class
    {
        var data = new Data();

        foreach (var field in value.GetType().GetFields())
        {
            var attr = field.GetCustomAttribute<JsonPropertyNameAttribute>();
            var name = attr?.Name ?? field.Name;
            data.Properties.Add(name, JsonSerializer.SerializeToElement(field.GetValue(value), options));
        }

        return data;
    }
}