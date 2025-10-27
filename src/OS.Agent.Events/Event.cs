using System.Text.Json;
using System.Text.Json.Serialization;

using OS.Agent.Storage.Models;

namespace OS.Agent.Events;

public abstract class Event(EntityType type, ActionType action)
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("key")]
    public virtual string Key => $"{Type}.{Action}";

    [JsonPropertyName("type")]
    public EntityType Type { get; init; } = type;

    [JsonPropertyName("action")]
    public ActionType Action { get; init; } = action;

    [JsonPropertyName("created_by")]
    public User? CreatedBy { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonExtensionData]
    public IDictionary<string, JsonElement> Properties { get; set; } = new Dictionary<string, JsonElement>();
}

[JsonConverter(typeof(Converter<EntityType>))]
public class EntityType(string value) : StringEnum(value)
{
    public static readonly EntityType Tenant = new("tenant");
    public bool IsTenant => Tenant.Equals(Value);

    public static readonly EntityType User = new("user");
    public bool IsUser => User.Equals(Value);

    public static readonly EntityType Account = new("account");
    public bool IsAccount => Account.Equals(Value);

    public static readonly EntityType Chat = new("chat");
    public bool IsChat => Chat.Equals(Value);

    public static readonly EntityType Message = new("message");
    public bool IsMessage => Message.Equals(Value);

    public static readonly EntityType Record = new("record");
    public bool IsRecord => Record.Equals(Value);

    public static readonly EntityType Log = new("log");
    public bool IsLog => Log.Equals(Value);

    public static readonly EntityType Install = new("install");
    public bool IsInstall => Install.Equals(Value);

    public static readonly EntityType Token = new("token");
    public bool IsToken => Token.Equals(Value);
}

[JsonConverter(typeof(Converter<ActionType>))]
public class ActionType(string value) : StringEnum(value)
{
    public static readonly ActionType Create = new("create");
    public bool IsCreate => Create.Equals(Value);

    public static readonly ActionType Update = new("update");
    public bool IsUpdate => Update.Equals(Value);

    public static readonly ActionType Delete = new("delete");
    public bool IsDelete => Delete.Equals(Value);

    public static readonly ActionType Resume = new("resume");
    public bool IsResume => Resume.Equals(Value);
}