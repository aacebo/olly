using System.Text.Json;

namespace OS.Agent.Storage.Models;

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