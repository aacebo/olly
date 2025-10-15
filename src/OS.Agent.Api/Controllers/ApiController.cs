using Microsoft.AspNetCore.Mvc;

using Octokit;

namespace OS.Agent.Api.Controllers;

[Route("/api")]
[ApiController]
public class ApiController(GitHubClient github) : ControllerBase
{
    private readonly DateTime _startedAt = DateTime.UtcNow;

    [HttpGet]
    public async Task<IResult> Get()
    {
        var curr = await github.GitHubApps.GetCurrent();
        return Results.Json(new Dictionary<string, object>()
        {
            { "startAt", _startedAt },
            { "github", curr }
        });
    }
}