using YamlDotNet.Serialization;

namespace OS.Agent.Drivers.Github.Settings;

/// <summary>
/// Github Repository Settings
/// <example>
/// <code>
/// links:
///     - https://github.com/test/a
///     - https://github.com/test/b
/// index:
///     - ./src/typescript/**/*.ts
///     - ./src/csharp/**/*.cs
/// </code>
/// </example>
/// </summary>
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