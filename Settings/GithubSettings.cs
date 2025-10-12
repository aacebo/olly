namespace OS.Agent.Settings;

public class GithubSettings
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string RedirectUrl { get; init; } = "https://aacebo.ngrok.io/api/github/redirect";
    public string OAuthUrl => $"https://github.com/login/oauth/authorize?client_id={ClientId}&redirect_uri={RedirectUrl}";
}