using Olly.Events;

namespace Olly.Contexts;

public interface IOllyContextFactory
{
    Task<OllyContext> Create(IEvent @event, CancellationToken cancellationToken = default);
}