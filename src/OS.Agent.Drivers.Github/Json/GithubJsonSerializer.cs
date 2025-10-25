using System.Text;
using System.Text.Json;

namespace OS.Agent.Drivers.Github.Json;

public static class GithubJsonSerializer
{
    private static Octokit.Internal.SimpleJsonSerializer Serializer { get; } = new();

    public static string Serialize<T>(T value)
    {
        return Serializer.Serialize(value);
    }

    public static JsonElement SerializeToElement<T>(T value)
    {
        string json = Serializer.Serialize(value);
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
        return JsonElement.ParseValue(ref reader);
    }

    public static T Deserialize<T>(string json)
    {
        return Serializer.Deserialize<T>(json);
    }
}