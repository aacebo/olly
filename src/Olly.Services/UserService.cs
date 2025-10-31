using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NetMQ;

using Olly.Events;
using Olly.Storage;
using Olly.Storage.Models;

namespace Olly.Services;

public interface IUserService
{
    Task<User?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<User> Create(User value, CancellationToken cancellationToken = default);
    Task<User> Update(User value, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class UserService(IServiceProvider provider) : IUserService
{
    private IMemoryCache Cache { get; init; } = provider.GetRequiredService<IMemoryCache>();
    private NetMQQueue<UserEvent> Events { get; init; } = provider.GetRequiredService<NetMQQueue<UserEvent>>();
    private IUserStorage Storage { get; init; } = provider.GetRequiredService<IUserStorage>();

    public async Task<User?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var user = Cache.Get<User>(id);

        if (user is not null)
        {
            return user;
        }

        user = await Storage.GetById(id, cancellationToken);

        if (user is not null)
        {
            Cache.Set(user.Id, user);
        }

        return user;
    }

    public async Task<User> Create(User value, CancellationToken cancellationToken = default)
    {
        var user = await Storage.Create(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Create)
        {
            User = user
        });

        return user;
    }

    public async Task<User> Update(User value, CancellationToken cancellationToken = default)
    {
        var user = await Storage.Update(value, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Update)
        {
            User = user
        });

        return user;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetById(id, cancellationToken) ?? throw new Exception("user not found");

        await Storage.Delete(id, cancellationToken: cancellationToken);

        Events.Enqueue(new(ActionType.Delete)
        {
            User = user
        });
    }
}