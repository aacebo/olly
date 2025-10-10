namespace OS.Agent.Models;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ModelAttribute : Attribute
{
    public string? Name { get; set; }
}