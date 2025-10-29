using System.Text.Json.Serialization;

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
[YamlSerializable]
public class GithubRepositorySettings
{
    /// <summary>
    /// OpenAI Model
    /// </summary>
    [YamlMember(Alias = "model")]
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// parent repository link
    /// </summary>
    [YamlMember(Alias = "parent")]
    [JsonPropertyName("parent")]
    public string? Parent { get; set; }

    /// <summary>
    /// documentation repository link
    /// </summary>
    [YamlMember(Alias = "documentation")]
    [JsonPropertyName("documentation")]
    public string? Documentation { get; set; }

    /// <summary>
    /// feature flag settings
    /// </summary>
    [YamlMember(Alias = "features")]
    [JsonPropertyName("features")]
    public GithubRepositoryFeatures Features { get; set; } = new();

    /// <summary>
    /// peer repository links
    /// </summary>
    [YamlMember(Alias = "peers")]
    [JsonPropertyName("peers")]
    public IList<string> Peers { get; set; } = [];

    /// <summary>
    /// file paths patterns to index
    /// </summary>
    [YamlMember(Alias = "index")]
    [JsonPropertyName("index")]
    public IList<string> Index { get; set; } = [];
}

[YamlSerializable]
public class GithubRepositoryFeatures
{
    [YamlMember(Alias = "issues")]
    [JsonPropertyName("issues")]
    public bool Issues { get; set; } = false;

    [YamlMember(Alias = "discussions")]
    [JsonPropertyName("discussions")]
    public bool Discussions { get; set; } = false;

    [YamlMember(Alias = "pull_requests")]
    [JsonPropertyName("pull_requests")]
    public bool PullRequests { get; set; } = false;
}