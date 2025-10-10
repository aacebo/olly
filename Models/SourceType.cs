using System.Text.Json.Serialization;

namespace OS.Agent.Models;

[JsonConverter(typeof(Converter<SourceType>))]
public class SourceType(string value) : StringEnum(value)
{
    public static readonly SourceType Github = new("github");
    public bool IsGithub => Github.Equals(Value);

    public static readonly SourceType Teams = new("teams");
    public bool IsTeams => Teams.Equals(Value);
}