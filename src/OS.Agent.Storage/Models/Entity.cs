using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.More;

namespace OS.Agent.Storage.Models;

[JsonConverter(typeof(EntityJsonConverterFactory))]
public class Entity : IDictionary<string, JsonElement>
{
    public string Type
    {
        get => GetRequired<string>("type");
        set => this["type"] = JsonSerializer.SerializeToElement(value);
    }

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

    public Entity()
    {
        Type = "any";
    }

    public Entity(string type = "any")
    {
        Type = type;
    }

    public Entity(Entity entity)
    {
        Type = entity.Type;
        Properties = entity.Properties;
    }

    public JsonElement Get(string path)
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

    public T? Get<T>(string path)
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

        return value.Deserialize<T>();
    }

    public T GetRequired<T>(string path)
    {
        return Get<T>(path) ?? throw new Exception($"'{path}' not found");
    }

    public static Entity From<T>(T value) where T : class
    {
        var data = new Entity();

        foreach (var field in value.GetType().GetFields())
        {
            var attr = field.GetCustomAttribute<JsonPropertyNameAttribute>();
            var name = attr?.Name ?? field.Name;
            data.Properties.Add(name, JsonSerializer.SerializeToElement(field.GetValue(value), field.FieldType));
        }

        return data;
    }

    public JsonElement? GetOrDefault(string key) => Properties.TryGetValue(key, out var value) ? value : null;
    public T? GetOrDefault<T>(string key) => Properties.TryGetValue(key, out var value) ? value.Deserialize<T>() : default;
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

public class Entities : List<Entity>, IList<Entity>
{
    public bool Has<TEntity>() where TEntity : Entity
    {
        var type = typeof(TEntity);
        return this.Any(entity => entity.GetType() == type);
    }

    public TEntity? Get<TEntity>() where TEntity : Entity
    {
        var type = typeof(TEntity);
        return (TEntity?)this.FirstOrDefault(entity => entity.GetType() == type);
    }

    public TEntity GetRequired<TEntity>() where TEntity : Entity
    {
        var type = typeof(TEntity);
        var entity = (TEntity?)this.FirstOrDefault(entity => entity.GetType() == type);
        return entity ?? throw new Exception($"entity type '{type}' not found");
    }

    public void Put<TEntity>(TEntity entity) where TEntity : Entity
    {
        var type = entity.GetType();
        var i = FindIndex(entity => entity.GetType() == type);

        if (i == -1)
        {
            Add(entity);
            return;
        }

        this[i] = entity;
    }
}

public class EntityJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type type)
    {
        return type == typeof(Entity) || type.IsSubclassOf(typeof(Entity));
    }

    public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options)
    {
        var converterType = typeof(EntityJsonConverter<>).MakeGenericType(type);
        var instance = Activator.CreateInstance(converterType) ?? throw new JsonException();
        return (JsonConverter)instance;
    }
}

public class EntityJsonConverter<TEntity> : JsonConverter<TEntity> where TEntity : Entity, new()
{
    public override TEntity? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);

        if (data is null) return null;

        var entity = new TEntity
        {
            Properties = data
        };

        return data is null ? null : entity;
    }

    public override void Write(Utf8JsonWriter writer, TEntity value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.ToDictionary(), options);
    }
}