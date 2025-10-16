namespace OS.Agent.Json;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class JsonDerivedFromTypeAttribute(Type type, string discriminator) : Attribute
{
    public Type From { get; init; } = type;
    public string Descriminator { get; init; } = discriminator;
}