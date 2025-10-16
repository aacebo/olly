using System.Text.Json;
using System.Text.Json.Serialization;

using FluentMigrator.Runner;

using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Models.OpenAI.Extensions;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Extensions.Logging;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using NetMQ;

using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;

using OS.Agent.Api.Controllers.Teams;
using OS.Agent.Api.Middleware;
using OS.Agent.Api.Webhooks;
using OS.Agent.Drivers;
using OS.Agent.Drivers.Github;
using OS.Agent.Drivers.Github.Extensions;
using OS.Agent.Drivers.Teams;
using OS.Agent.Drivers.Teams.Extensions;
using OS.Agent.Events;
using OS.Agent.Json;
using OS.Agent.Services;
using OS.Agent.Storage;
using OS.Agent.Storage.Extensions;
using OS.Agent.Storage.Models;
using OS.Agent.Workers;

var builder = WebApplication.CreateBuilder(args);
var openAiSettings = builder.Configuration.GetOpenAI();
var openAiModel = new OpenAIChatModel(openAiSettings.Model, openAiSettings.ApiKey);
var jsonSerializerOptions = new JsonSerializerOptions()
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowOutOfOrderMetadataProperties = true,
    TypeInfoResolver = new CustomTypeInfoResolver()
};

builder.Services.Configure<GithubSettings>(builder.Configuration.GetSection("Github"));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = jsonSerializerOptions.DefaultIgnoreCondition;
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = jsonSerializerOptions.AllowOutOfOrderMetadataProperties;
    options.SerializerOptions.TypeInfoResolver = jsonSerializerOptions.TypeInfoResolver;
});

builder.Services.AddSingleton(provider => jsonSerializerOptions);
builder.Services.AddOpenApi();
builder.Services.AddHttpLogging();
builder.Services.AddPostgres();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// Teams
builder.AddTeams();
builder.AddTeamsDevTools();
builder.Services.AddTeamsDriver();
builder.Services.AddScoped<TeamsDriver>();
builder.Services.AddScoped<IDriver>(provider => provider.GetRequiredService<TeamsDriver>());
builder.Services.AddScoped<IChatDriver>(provider => provider.GetRequiredService<TeamsDriver>());

// Github
builder.Services.AddGithubDriver();

// AI
builder.Services.AddSingleton(openAiModel);

// Controllers
builder.Services.AddScoped<ErrorMiddleware>();
builder.Services.AddScoped<ChatController>();
builder.Services.AddScoped<InstallController>();
builder.Services.AddScoped<MessageController>();

// Queues
builder.Services.AddSingleton<NetMQQueue<Event<UserEvent>>>(); // users.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<Event<TenantEvent>>>(); // tenants.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<Event<AccountEvent>>>(); // accounts.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<Event<ChatEvent>>>(); // chats.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<Event<MessageEvent>>>(); // messages.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<Event<TokenEvent>>>(); // tokens.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<Event<EntityEvent>>>(); // entities.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<Event<LogEvent>>>(); // logs.create

// Webhooks
builder.Services.AddSingleton<WebhookEventProcessor, GithubInstallProcessor>();

// Workers
builder.Services.AddHostedService<MessageWorker>();

// Storage
builder.Services.AddScoped<IStorage, Storage>();
builder.Services.AddScoped<IUserStorage, UserStorage>();
builder.Services.AddScoped<ITenantStorage, TenantStorage>();
builder.Services.AddScoped<IAccountStorage, AccountStorage>();
builder.Services.AddScoped<IChatStorage, ChatStorage>();
builder.Services.AddScoped<IMessageStorage, MessageStorage>();
builder.Services.AddScoped<IEntityStorage, EntityStorage>();
builder.Services.AddScoped<ITokenStorage, TokenStorage>();
builder.Services.AddScoped<ILogStorage, LogStorage>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ILogService, LogService>();

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