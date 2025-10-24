using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OS.Agent.Drivers;
using OS.Agent.Services;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Contexts;

/// <summary>
/// The base interface for all other context types
/// </summary>
public abstract class AgentContext<TDriver> where TDriver : IDriver
{
    public IServiceProvider Provider { get; }
    public IServices Services { get; }
    public IStorage Storage { get; }
    public JsonSerializerOptions JsonSerializerOptions { get; }
    public CancellationToken CancellationToken { get; }

    protected SourceType Type { get; }
    protected TDriver Driver { get; }
    protected ILogger<AgentContext<TDriver>> Logger { get; }

    public AgentContext(SourceType type, IServiceScopeFactory factory) : this(type, factory.CreateScope())
    {
        Type = type;
    }

    public AgentContext(SourceType type, IServiceScope scope, CancellationToken cancellationToken = default)
    {
        Provider = scope.ServiceProvider;
        Type = type;
        Services = scope.ServiceProvider.GetRequiredService<IServices>();
        Storage = scope.ServiceProvider.GetRequiredService<IStorage>();
        JsonSerializerOptions = scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        Logger = scope.ServiceProvider.GetRequiredService<ILogger<AgentContext<TDriver>>>();
        Driver = scope.ServiceProvider.GetRequiredKeyedService<TDriver>(type.ToString());
        CancellationToken = cancellationToken;
    }
}