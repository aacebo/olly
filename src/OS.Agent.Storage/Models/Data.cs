using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.More;

namespace OS.Agent.Storage.Models;

[JsonPolymorphic]
[JsonDerivedType(typeof(Data), "data")]
public class Data : IDictionary<string, JsonElement>
{
    [JsonIgnore]
    public ICollection<string> Keys => Properties.Keys;

    [JsonIgnore]
    public ICollection<JsonElement> Values => Properties.Values;

    [JsonIgnore]
    public int Count => Properties.Count;

    [JsonIgnore]
    public bool IsReadOnly => Properties.IsReadOnly;

    [JsonIgnore]
    public JsonElement this[string key]
    {
        get => Properties[key];
        set => Properties[key] = value;
    }

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

    public object? GetOrDefault(string key) => Properties.TryGetValue(key, out JsonElement value) ? value : null;
    public T? GetOrDefault<T>(string key) => Properties.TryGetValue(key, out JsonElement value) ? value.Deserialize<T>() : default;
    public void Add(string key, JsonElement value) => Properties.Add(key, value);
    public bool ContainsKey(string key) => Properties.ContainsKey(key);
    public bool Remove(string key) => Properties.Remove(key);
    public bool TryGetValue(string key, out JsonElement value) => Properties.TryGetValue(key, out value);
    public void Add(KeyValuePair<string, JsonElement> item) => Properties.Add(item);
    public void Clear() => Properties.Clear();
    public bool Contains(KeyValuePair<string, JsonElement> item) => Properties.Contains(item);
    public void CopyTo(KeyValuePair<string, JsonElement>[] array, int arrayIndex) => Properties.CopyTo(array, arrayIndex);
    public bool Remove(KeyValuePair<string, JsonElement> item) => Properties.Remove(item);
    public IEnumerator<KeyValuePair<string, JsonElement>> GetEnumerator() => Properties.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}