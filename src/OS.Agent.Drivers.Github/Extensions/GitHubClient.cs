using System.Text;

using OS.Agent.Drivers.Github.Settings;

namespace OS.Agent.Drivers.Github.Extensions;

public static class GitHubClientExtensions
{
    public static async Task<GithubRepositorySettings?> GetOllySettings(this Octokit.IRepositoryContentsClient client, string owner, string name)
    {
        var serializer = new YamlDotNet.Serialization.Deserializer();

        try
        {
            var res = await client.GetRawContent(owner, name, ".github/olly.yml");
            var settings = serializer.Deserialize<GithubRepositorySettings>(Encoding.UTF8.GetString(res));
            return settings;
        }
        catch
        {
            return null;
        }
    }
}