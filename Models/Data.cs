using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.More;

namespace OS.Agent.Models;

[JsonPolymorphic]
[JsonDerivedType(typeof(Data), typeDiscriminator: "data")]
[JsonDerivedType(typeof(AccountData), typeDiscriminator: "account")]
[JsonDerivedType(typeof(TeamsAccountData), typeDiscriminator: "account.teams")]
[JsonDerivedType(typeof(GithubAccountData), typeDiscriminator: "account.github")]
[JsonDerivedType(typeof(ChatData), typeDiscriminator: "chat")]
[JsonDerivedType(typeof(TeamsChatData), typeDiscriminator: "chat.teams")]
[JsonDerivedType(typeof(MessageData), typeDiscriminator: "message")]
[JsonDerivedType(typeof(TeamsMessageData), typeDiscriminator: "message.teams")]
public class Data(params IEnumerable<KeyValuePair<string, JsonElement>> pairs)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>(pairs);

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public object? Get(string path) => Get<object>(path);

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

        return value.Deserialize<T>(JsonOptions);
    }

    public static Data From<T>(T value) where T : class
    {
        var data = new Data();

        foreach (var field in value.GetType().GetFields())
        {
            var attr = field.GetCustomAttribute<JsonPropertyNameAttribute>();
            var name = attr?.Name ?? field.Name;
            data.Properties.Add(name, JsonSerializer.SerializeToElement(field.GetValue(value), JsonOptions));
        }

        return data;
    }

    public static JsonSerializerOptions JsonOptions => new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowOutOfOrderMetadataProperties = true
    };
}