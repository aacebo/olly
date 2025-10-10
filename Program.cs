using System.Text.Json.Serialization;

using FluentMigrator.Runner;

using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Extensions.Logging;

using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using NetMQ;

using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using Octokit.Webhooks.Models;

using OS.Agent;
using OS.Agent.Extensions;
using OS.Agent.Models;
using OS.Agent.Postgres;
using OS.Agent.Settings;
using OS.Agent.Stores;
using OS.Agent.Webhooks;
using OS.Agent.Workers;

var builder = WebApplication.CreateBuilder(args);
var pgUrl = builder.Configuration.GetConnectionString("Postgres") ??
    throw new Exception("ConnectionStrings.Postgres not found");

builder.Services.Configure<GithubSettings>(builder.Configuration.GetSection("Github"));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddOpenApi();
builder.AddTeams();
builder.AddTeamsDevTools();
builder.Services.AddGithubClient();
builder.Services.AddPostgres(pgUrl);
builder.Services.AddTransient<MainController>();
builder.Services.AddSingleton<NetMQQueue<Event<Installation>>>();
builder.Services.AddHostedService<InstallWorker>();
builder.Services.AddSingleton<WebhookEventProcessor, InstallProcessor>();

builder.Services.AddScoped<IUserStorage, UserStorage>();
builder.Services.AddScoped<ITenantStorage, TenantStorage>();
builder.Services.AddScoped<IAccountStorage, AccountStorage>();
builder.Services.AddScoped<IChatStorage, ChatStorage>();
builder.Services.AddScoped<IMessageStorage, MessageStorage>();
builder.Services.AddScoped<IEntityStorage, EntityStorage>();

Dapper.SqlMapper.AddTypeHandler(new JsonDocumentTypeHandler());
Dapper.SqlMapper.AddTypeHandler(new StringEnumTypeHandler<SourceType>());

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.MapOpenApi();
}

app.Services.GetRequiredService<IMigrationRunner>().MigrateUp();
app.MapGitHubWebhooks();
app.UseTeams();
app.Run();