using FluentMigrator.Runner;

using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Extensions.Logging;

using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using NetMQ;

using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;

using OS.Agent;
using OS.Agent.Extensions;
using OS.Agent.Models;
using OS.Agent.Settings;
using OS.Agent.Stores;
using OS.Agent.Webhooks;
using OS.Agent.Workers;

var builder = WebApplication.CreateBuilder(args);
var pgUrl = builder.Configuration.GetConnectionString("Postgres") ??
    throw new Exception("ConnectionStrings.Postgres not found");

builder.Services.Configure<GithubSettings>(builder.Configuration.GetSection("Github"));
builder.Services.Configure<ZeroMQSettings>(builder.Configuration.GetSection("ZeroMQ"));
builder.Services.AddOpenApi();
builder.AddTeams();
builder.AddTeamsDevTools();
builder.Services.AddGithubClient();
builder.Services.AddPostgres(pgUrl);
builder.Services.AddTransient<MainController>();
builder.Services.AddSingleton<NetMQQueue<IEvent>>();
builder.Services.AddSingleton<WebhookEventProcessor, InstallProcessor>();
builder.Services.AddHostedService<InstallWorker>();
builder.Services.AddSingleton<IUserStorage, UserStorage>();
builder.Services.AddSingleton<IAccountStorage, AccountStorage>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.MapOpenApi();
}

app.Services.GetRequiredService<IMigrationRunner>().MigrateUp();
app.MapGitHubWebhooks();
app.UseTeams();
app.Run();