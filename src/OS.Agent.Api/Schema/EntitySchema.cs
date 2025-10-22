using System.Reflection;
using System.Text.Json.Serialization;

namespace OS.Agent.Api.Schema;

[GraphQLName("Entity")]
public class EntitySchema
{
    [GraphQLName("type")]
    public string Type { get; set; }

    [GraphQLType(typeof(AnyType))]
    [GraphQLName("properties")]
    public IDictionary<string, object?> Properties { get; set; }

    public EntitySchema(Storage.Models.Entity entity)
    {
        Type = entity.Type;
        Properties = new Dictionary<string, object?>();

        foreach (var property in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty))
        {
            if (property.Name != "Type")
            {
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
                {
                    continue;
                }

                var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                Properties[name] = property.GetValue(entity);
            }
        }
    }
}