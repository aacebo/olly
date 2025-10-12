using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Models;

[Model]
public class Token : Model
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("account_id")]
    [JsonPropertyName("account_id")]
    public required Guid AccountId { get; init; }

    [Column("type")]
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [Column("access_token")]
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [Column("refresh_token")]
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [Column("expires_at")]
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [Column("refresh_expires_at")]
    [JsonPropertyName("refresh_expires_at")]
    public DateTimeOffset? RefreshExpiresAt { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Ignore]
    [JsonIgnore]
    public bool IsExpired => ExpiresAt is not null && ExpiresAt.Value > DateTimeOffset.UtcNow;

    [Ignore]
    [JsonIgnore]
    public bool IsRefreshExpired => RefreshExpiresAt is not null && RefreshExpiresAt.Value > DateTimeOffset.UtcNow;

    public class State
    {
        [JsonPropertyName("tenant_id")]
        public required Guid TenantId { get; set; }

        [JsonPropertyName("user_id")]
        public required Guid UserId { get; set; }

        [JsonPropertyName("account_id")]
        public Guid? AccountId { get; set; }

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