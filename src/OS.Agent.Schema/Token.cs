using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OS.Agent.Schema;

public class Token
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("account")]
    public required AccountPartial Account { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [JsonPropertyName("refresh_expires_at")]
    public DateTimeOffset? RefreshExpiresAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    public bool IsExpired => ExpiresAt is not null && ExpiresAt.Value > DateTimeOffset.UtcNow;

    [JsonIgnore]
    public bool IsRefreshExpired => RefreshExpiresAt is not null && RefreshExpiresAt.Value > DateTimeOffset.UtcNow;

    public class State
    {
        [JsonPropertyName("tenant_id")]
        public required Guid TenantId { get; set; }

        [JsonPropertyName("user_id")]
        public required Guid UserId { get; set; }

        [JsonPropertyName("message_id")]
        public required Guid MessageId { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public string Encode()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(ToString()));
        }

        public static State Decode(string state)
        {
            var str = Encoding.UTF8.GetString(Convert.FromBase64String(state));
            return JsonSerializer.Deserialize<State>(str) ?? throw new Exception("invalid state");
        }
    }
}