using System.Text.Json;
using System.Text.Json.Serialization;

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
public class Data
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement> Properties = new Dictionary<string, JsonElement>();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowOutOfOrderMetadataProperties = true
        });
    }
}