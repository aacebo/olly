using System.Text;

using Microsoft.Extensions.Logging;

using OS.Agent.Drivers.Github.Settings;

using YamlDotNet.Core;

namespace OS.Agent.Drivers.Github.Extensions;

public static class GitHubClientExtensions
{
    public static async Task<GithubRepositorySettings?> GetOllySettings(this Octokit.IRepositoryContentsClient client, string owner, string name, ILogger? logger = null)
    {
        var serializer = new YamlDotNet.Serialization.Deserializer();

        try
        {
            var res = await client.GetRawContent(owner, name, ".github/olly.yml");
            var settings = serializer.Deserialize<GithubRepositorySettings>(Encoding.UTF8.GetString(res));
            return settings;
        }
        catch (Exception ex)
        {
            if (ex is YamlException yaml)
            {
                logger?.LogWarning("failed to deserialize repository settings {}", yaml);
            }

            return null;
        }
    }
}