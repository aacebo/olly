using System.Text.Json;
using System.Text.Json.Serialization;

using FluentMigrator.Runner;

using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Models.OpenAI.Extensions;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Extensions.Logging;

using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using NetMQ;

using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;

using OS.Agent.Controllers.Teams;
using OS.Agent.Events;
using OS.Agent.Extensions;
using OS.Agent.Middleware;
using OS.Agent.Models;
using OS.Agent.Settings;
using OS.Agent.Stores;
using OS.Agent.Webhooks;
using OS.Agent.Workers;

var builder = WebApplication.CreateBuilder(args);
var openAiSettings = builder.Configuration.GetOpenAI();
var openAiModel = new OpenAIChatModel(openAiSettings.Model, openAiSettings.ApiKey);

builder.Services.Configure<GithubSettings>(builder.Configuration.GetSection("Github"));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSingleton(new JsonSerializerOptions()
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
});

builder.Services.AddOpenApi();
builder.Services.AddHttpLogging();
builder.Services.AddGithubClient();
builder.Services.AddPostgres();

// Teams
builder.AddTeams();
builder.AddTeamsDevTools();

// AI
builder.Services.AddSingleton(openAiModel);

// Controllers
builder.Services.AddTransient<ErrorMiddleware>();
builder.Services.AddTransient<ChatController>();
builder.Services.AddTransient<InstallController>();
builder.Services.AddTransient<MessageController>();

// Queues
builder.Services.AddSingleton<NetMQQueue<Event<GithubInstallEvent>>>();
builder.Services.AddSingleton<NetMQQueue<Event<MessageActivity>>>();

// Webhooks
builder.Services.AddSingleton<WebhookEventProcessor, GithubInstallProcessor>();

// Workers
builder.Services.AddHostedService<GithubInstallWorker>();
builder.Services.AddHostedService<MessageActivityWorker>();

// Storage
builder.Services.AddScoped<IStorage, Storage>();
builder.Services.AddScoped<IUserStorage, UserStorage>();
builder.Services.AddScoped<ITenantStorage, TenantStorage>();
builder.Services.AddScoped<IAccountStorage, AccountStorage>();
builder.Services.AddScoped<IChatStorage, ChatStorage>();
builder.Services.AddScoped<IMessageStorage, MessageStorage>();
builder.Services.AddScoped<IEntityStorage, EntityStorage>();
builder.Services.AddScoped<ITokenStorage, TokenStorage>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.MapOpenApi();
}

app.Services.GetRequiredService<IMigrationRunner>().MigrateUp();
app.UseMiddleware<ErrorMiddleware>();
app.MapGitHubWebhooks();
app.UseTeams();
app.Run();