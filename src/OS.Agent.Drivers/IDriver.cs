using OS.Agent.Drivers.Models;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers;

public interface IDriver
{
    SourceType Type { get; }

    Task SignIn(SignInRequest request, CancellationToken cancellationToken = default);
}

public interface IChatDriver : IDriver
{
    Task Typing(TypingRequest request, CancellationToken cancellationToken = default);
    Task<Message> Send(MessageRequest request, CancellationToken cancellationToken = default);
    Task<Message> Reply(MessageReplyRequest request, CancellationToken cancellationToken = default);
}