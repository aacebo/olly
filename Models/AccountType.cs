using System.Text.Json.Serialization;

namespace OS.Agent.Models;

[JsonConverter(typeof(Converter<AccountType>))]
public class AccountType(string value) : StringEnum(value)
{
    public static readonly AccountType Github = new("github");
    public bool IsGithub => Github.Equals(Value);

    public static readonly AccountType Teams = new("teams");
    public bool IsTeams => Teams.Equals(Value);
}