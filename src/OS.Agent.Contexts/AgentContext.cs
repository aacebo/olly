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

    public AgentContext(SourceType type, IServiceScopeFactory factory) : this(type, factory.CreateScope().ServiceProvider)
    {
        Type = type;
    }

    public AgentContext(SourceType type, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        Provider = provider;
        Type = type;
        Services = provider.GetRequiredService<IServices>();
        Storage = provider.GetRequiredService<IStorage>();
        JsonSerializerOptions = provider.GetRequiredService<JsonSerializerOptions>();
        Logger = provider.GetRequiredService<ILogger<AgentContext<TDriver>>>();
        Driver = provider.GetRequiredKeyedService<TDriver>(type.ToString());
        CancellationToken = cancellationToken;
    }
}