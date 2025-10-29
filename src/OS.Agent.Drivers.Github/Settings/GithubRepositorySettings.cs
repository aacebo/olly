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
public class GithubRepositorySettings
{
    /// <summary>
    /// OpenAI Model
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// parent repository link
    /// </summary>
    [JsonPropertyName("parent")]
    public string? Parent { get; set; }

    /// <summary>
    /// documentation repository link
    /// </summary>
    [JsonPropertyName("documentation")]
    public string? Documentation { get; set; }

    /// <summary>
    /// feature flag settings
    /// </summary>
    [JsonPropertyName("features")]
    public GithubRepositoryFeatures Features { get; set; } = new();

    /// <summary>
    /// peer repository links
    /// </summary>
    [JsonPropertyName("peers")]
    public IList<string> Peers { get; set; } = [];

    /// <summary>
    /// file paths patterns to index
    /// </summary>
    [YamlMember(Alias = "index")]
    public IList<string> Index { get; set; } = [];
}

public class GithubRepositoryFeatures
{
    [JsonPropertyName("issues")]
    public bool Issues { get; set; } = false;

    [JsonPropertyName("discussions")]
    public bool Discussions { get; set; } = false;

    [JsonPropertyName("pull_requests")]
    public bool PullRequests { get; set; } = false;
}