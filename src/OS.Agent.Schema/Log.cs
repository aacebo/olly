using System.Text.Json.Serialization;

namespace OS.Agent.Schema;

public class Log
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("tenant_id")]
    public required TenantPartial Tenant { get; init; }

    [JsonPropertyName("level")]
    public LogLevel Level { get; init; } = LogLevel.Info;

    [JsonPropertyName("type")]
    public LogType Type { get; init; } = LogType.System;

    [JsonPropertyName("type_id")]
    public string? TypeId { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("entities")]
    public Entities Entities { get; init; } = [];

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

[JsonConverter(typeof(Converter<LogLevel>))]
public class LogLevel(string value) : StringEnum(value)
{
    public static readonly LogLevel Debug = new("debug");
    public bool IsDebug => Debug.Equals(Value);

    public static readonly LogLevel Info = new("info");
    public bool IsInfo => Info.Equals(Value);

    public static readonly LogLevel Warn = new("warn");
    public bool IsWarn => Warn.Equals(Value);

    public static readonly LogLevel Error = new("error");
    public bool IsError => Error.Equals(Value);
}

[JsonConverter(typeof(Converter<LogType>))]
public class LogType(string value) : StringEnum(value)
{
    public static readonly LogType System = new("system");
    public bool IsSystem => System.Equals(Value);

    public static readonly LogType Tenant = new("tenant");
    public bool IsTenant => Tenant.Equals(Value);

    public static readonly LogType Account = new("account");
    public bool IsAccount => Account.Equals(Value);

    public static readonly LogType Chat = new("chat");
    public bool IsChat => Chat.Equals(Value);

    public static readonly LogType Message = new("message");
    public bool IsMessage => Message.Equals(Value);

    public static readonly LogType Entity = new("entity");
    public bool IsEntity => Entity.Equals(Value);

    public static readonly LogType Agent = new("agent");
    public bool IsAgent => Agent.Equals(Value);
}