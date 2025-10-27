using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OS.Agent.Services;
using OS.Agent.Storage;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers;

public abstract class DriverClient : IClient
{
    public IServiceProvider Provider { get; }
    public IServices Services { get; }
    public IStorage Storage { get; }
    public JsonSerializerOptions JsonSerializerOptions { get; }
    public CancellationToken CancellationToken { get; }
    public SourceType Type { get; }

    protected ILogger<DriverClient> Logger { get; }
    protected DriverResponse Response { get; } = new();

    public DriverClient(SourceType type, IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        Provider = provider;
        Type = type;
        Services = provider.GetRequiredService<IServices>();
        Storage = provider.GetRequiredService<IStorage>();
        JsonSerializerOptions = provider.GetRequiredService<JsonSerializerOptions>();
        Logger = provider.GetRequiredService<ILogger<DriverClient>>();
        CancellationToken = cancellationToken;
    }
}