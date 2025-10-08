using FluentMigrator.Runner;

using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Extensions.Logging;

using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;

using OS.Agent;
using OS.Agent.Extensions;
using OS.Agent.Settings;
using OS.Agent.Webhooks;

var builder = WebApplication.CreateBuilder(args);
var pgUrl = builder.Configuration.GetConnectionString("Postgres") ??
    throw new Exception("ConnectionStrings.Postgres not found");

builder.Services.Configure<GithubSettings>(builder.Configuration.GetSection("Github"));
builder.Services.AddOpenApi();
builder.AddTeams();
builder.AddTeamsDevTools();
builder.Services.AddTransient<MainController>();
builder.Services.AddSingleton<WebhookEventProcessor, InstallProcessor>();
builder.Services.AddGithubClient();
builder.Services.AddPostgres(pgUrl);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.MapOpenApi();
}

app.Services
    .GetRequiredService<IMigrationRunner>()
    .MigrateUp();

app.MapGitHubWebhooks();
app.UseTeams();
app.Run();