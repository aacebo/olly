using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Json.More;

namespace Olly.Storage.Models;

[JsonConverter(typeof(EntityJsonConverter))]
public class Entity
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

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

    public static Entity From<T>(T value)
    {
        var properties = JsonSerializer.SerializeToDocument(value).Deserialize<Dictionary<string, JsonElement>>();

        return new()
        {
            Properties = properties ?? []
        };
    }
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

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public class EntityAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

public class EntityJsonConverter : JsonConverter<Entity>
{
    public override Entity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ?? throw new JsonException();

        if (!element.TryGetPropertyValue("type", out var type) || type is null)
        {
            throw new JsonException();
        }

        // default to base Entity
        if (!EntityTypeRegistry.Types.TryGetValue(type.ToString(), out var entityType))
        {
            return new(type.ToString())
            {
                Properties = element.Deserialize<Dictionary<string, JsonElement>>(options) ?? throw new JsonException()
            };
        }

        var entity = JsonSerializer.Deserialize(element.AsJsonString(options), entityType, options) as Entity;
        return entity;
    }

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
    {
        if (EntityTypeRegistry.Types.TryGetValue(value.Type, out var entityType))
        {
            JsonSerializer.Serialize(writer, value, options.GetTypeInfo(entityType));
            return;
        }

        var properties = value.Properties.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
        );

        properties["type"] = JsonSerializer.SerializeToElement(value.Type);
        JsonSerializer.Serialize(writer, properties, options);
    }
}

internal static class EntityTypeRegistry
{
    public static Dictionary<string, Type> Types = [];
}