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

using Olly.Api.Controllers.Teams;
using Olly.Api.Middleware;
using Olly.Api.Webhooks;
using Olly.Drivers.Github.Extensions;
using Olly.Drivers.Github.Settings;
using Olly.Drivers.Teams.Extensions;
using Olly.Events;
using Olly.Services;
using Olly.Storage;
using Olly.Storage.Extensions;
using Olly.Workers;

using Schema = Olly.Api.Schema;

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
    options.ServicesStartConcurrently = true;
    options.ServicesStopConcurrently = true;
    options.ShutdownTimeout = TimeSpan.FromSeconds(5);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = jsonSerializerOptions.DefaultIgnoreCondition;
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = jsonSerializerOptions.AllowOutOfOrderMetadataProperties;
});

builder.Services.AddSingleton(provider => jsonSerializerOptions);
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
builder.Services.AddSingleton<NetMQQueue<JobEvent>>(); // jobs.(create | update)
builder.Services.AddSingleton<NetMQQueue<DocumentEvent>>(); // documents.(create | update | delete)

// Webhooks
builder.Services.AddSingleton<WebhookEventProcessor, GithubDiscussionWebhook>();

// Workers
builder.Services.AddHostedService<MessageWorker>();
builder.Services.AddHostedService<InstallWorker>();
builder.Services.AddHostedService<JobWorker>();

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
builder.Services.AddScoped<IJobStorage, JobStorage>();
builder.Services.AddScoped<IDocumentStorage, DocumentStorage>();

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
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

var app = builder.Build();

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