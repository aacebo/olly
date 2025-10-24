using YamlDotNet.Serialization;

namespace OS.Agent.Drivers.Github.Settings;

public class GithubRepositorySettings
{
    /// <summary>
    /// linked repositories
    /// </summary>
    [YamlMember(Alias = "Links")]
    public IList<string> Links { get; set; } = [];

    /// <summary>
    /// file paths patterns to index
    /// </summary>
    [YamlMember(Alias = "index")]
    public IList<string> Index { get; set; } = [];
}