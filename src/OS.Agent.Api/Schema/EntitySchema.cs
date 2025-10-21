using System.Text.Json;

using Json.More;

namespace OS.Agent.Api.Schema;

[GraphQLName("Entity")]
public class EntitySchema
{
    [GraphQLName("type")]
    public string Type { get; set; }

    [GraphQLName("properties")]
    public IDictionary<string, JsonElement> Properties { get; set; }

    public EntitySchema(Storage.Models.Entity entity)
    {
        Type = entity.Type;
        Properties = new Dictionary<string, JsonElement>();

        foreach (var property in entity.ToJsonDocument().RootElement.EnumerateObject())
        {
            if (property.Name != "type")
            {
                Properties[property.Name] = property.Value;
            }
        }
    }
}