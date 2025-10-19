using System.Text.Json;

using Json.More;

namespace OS.Agent.Drivers.Github;

public class GithubJsonConverter<T> : WeaklyTypedJsonConverter<T>
{
    private Octokit.Internal.SimpleJsonSerializer Serializer { get; init; } = new();

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var asString = JsonDocument.ParseValue(ref reader).RootElement.GetRawText();
        return Serializer.Deserialize<T>(asString);
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        var json = Serializer.Serialize(value);
        writer.WriteRawValue(json);
    }
}