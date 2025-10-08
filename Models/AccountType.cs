using System.Text.Json.Serialization;

namespace OS.Agent.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountType
{
    [JsonStringEnumMemberName("teams")]
    Teams,

    [JsonStringEnumMemberName("github")]
    Github
}

public static class AccountTypeExtensions
{
    public static string ToString(this AccountType accountType)
    {
        return accountType switch
        {
            AccountType.Teams => "teams",
            AccountType.Github => "github",
            _ => throw new InvalidDataException("invalid AccountType")
        };
    }
}