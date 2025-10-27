using System.Text.Json;
using System.Text.Json.Serialization;

using FluentMigrator.Runner;

using Microsoft.Extensions.FileProviders;
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
using OS.Agent.Drivers.Github.Extensions;
using OS.Agent.Drivers.Github.Settings;
using OS.Agent.Drivers.Teams.Extensions;
using OS.Agent.Events;
using OS.Agent.Services;
using OS.Agent.Storage;
using OS.Agent.Storage.Extensions;
using OS.Agent.Workers;

using Schema = OS.Agent.Api.Schema;

var builder = WebApplication.CreateBuilder(args);
var openAiSettings = builder.Configuration.GetOpenAI();
var openAiModel = new OpenAIChatModel(openAiSettings.Model, openAiSettings.ApiKey);
var jsonSerializerOptions = new JsonSerializerOptions()
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowOutOfOrderMetadataProperties = true
};

builder.Services.Configure<GithubSettings>(builder.Configuration.GetSection("Github"));
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = jsonSerializerOptions.DefaultIgnoreCondition;
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = jsonSerializerOptions.AllowOutOfOrderMetadataProperties;
});

builder.Services.AddSingleton(provider => jsonSerializerOptions);
builder.Services.AddOpenApi();
builder.Services.AddHttpLogging();
builder.Services.AddPostgres();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddGraphQLServer()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = !builder.Environment.IsProduction())
    .AddQueryType<Schema.Query>();

// Teams
builder.AddTeams();
builder.AddTeamsDevTools();
builder.Services.AddTeamsDriver();

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
builder.Services.AddSingleton<NetMQQueue<UserEvent>>(); // users.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<TenantEvent>>(); // tenants.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<AccountEvent>>(); // accounts.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<ChatEvent>>(); // chats.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<MessageEvent>>(); // messages.(create | update | delete | resume)
builder.Services.AddSingleton<NetMQQueue<TokenEvent>>(); // tokens.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<RecordEvent>>(); // records.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<InstallEvent>>(); // installs.(create | update | delete)
builder.Services.AddSingleton<NetMQQueue<LogEvent>>(); // logs.create

// Webhooks
builder.Services.AddSingleton<WebhookEventProcessor, GithubInstallWebhook>();
builder.Services.AddSingleton<WebhookEventProcessor, GithubDiscussionWebhook>();

// Workers
builder.Services.AddHostedService<MessageWorker>();
builder.Services.AddHostedService<AccountWorker>();
builder.Services.AddHostedService<InstallWorker>();

// Storage
builder.Services.AddScoped<IStorage, Storage>();
builder.Services.AddScoped<IUserStorage, UserStorage>();
builder.Services.AddScoped<ITenantStorage, TenantStorage>();
builder.Services.AddScoped<IAccountStorage, AccountStorage>();
builder.Services.AddScoped<IChatStorage, ChatStorage>();
builder.Services.AddScoped<IMessageStorage, MessageStorage>();
builder.Services.AddScoped<ITokenStorage, TokenStorage>();
builder.Services.AddScoped<ILogStorage, LogStorage>();
builder.Services.AddScoped<IRecordStorage, RecordStorage>();
builder.Services.AddScoped<IInstallStorage, InstallStorage>();

// Services
builder.Services.AddScoped<IServices, Services>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IRecordService, RecordService>();
builder.Services.AddScoped<IInstallService, InstallService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.MapOpenApi();
}

app.UseStaticFiles(new StaticFileOptions()
{
    RequestPath = "/static",
    FileProvider = new PhysicalFileProvider(System.IO.Path.Combine(
        Directory.GetCurrentDirectory(),
        "Static"
    ))
});

app.Services.GetRequiredService<IMigrationRunner>().MigrateUp();
app.UseMiddleware<ErrorMiddleware>();
app.MapGitHubWebhooks();
app.MapEntityTypes();
app.MapTeamsEntityTypes();
app.MapGithubEntityTypes();
app.MapGraphQL();
app.UseTeams();
app.Run();