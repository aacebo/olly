using System.Collections.Concurrent;

using OS.Agent.Events;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers;

public static class ClientRegistry
{
    private static ConcurrentDictionary<string, ClientFactory> Collection { get; } = [];

    public static ClientFactory Get(SourceType sourceType)
    {
        if (!Collection.TryGetValue(sourceType, out var factory))
        {
            throw new InvalidOperationException($"client not found for source type '{sourceType}'");
        }

        return factory;
    }

    public static void Register(SourceType sourceType, ClientFactory factory)
    {
        Collection.AddOrUpdate(sourceType, factory, (_, _) => factory);
    }

    public delegate Client ClientFactory(Event @event, IServiceProvider provider, CancellationToken cancellationToken = default);
}