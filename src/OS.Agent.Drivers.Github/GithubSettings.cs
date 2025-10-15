namespace OS.Agent.Drivers.Github;

public class GithubSettings
{
    public required long AppId { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string RedirectUrl { get; init; } = "https://aacebo.ngrok.io/api/github/redirect";
    public string InstallUrl => $"https://github.com/apps/tos-agent/installations/new?redirect_uri={RedirectUrl}";
    // public string OAuthUrl => $"https://github.com/login/oauth/authorize?client_id={ClientId}&redirect_uri={RedirectUrl}";
}