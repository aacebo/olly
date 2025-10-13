using System.Text.Json;
using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Models;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ModelAttribute : Attribute
{
    public string? Name { get; set; }
}

public abstract class Model
{
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    public string ToString(JsonSerializerOptions options)
    {
        return JsonSerializer.Serialize(this, options);
    }
}

public abstract class Model<TData> : Model where TData : Data
{
    [Column("data")]
    [JsonPropertyName("data")]
    public required TData Data { get; set; }
}