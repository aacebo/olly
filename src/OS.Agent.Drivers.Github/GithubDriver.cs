using Microsoft.Extensions.DependencyInjection;

using Octokit.GraphQL;
using Octokit.GraphQL.Model;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Drivers.Models;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public class GithubDriver(IServiceProvider provider) : IChatDriver
{
    public SourceType Type => SourceType.Github;

    private Octokit.GitHubClient AppClient => provider.GetRequiredService<Octokit.GitHubClient>();
    private IAccountService Accounts => provider.GetRequiredService<IAccountService>();

    public Task SignIn(SignInRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task Typing(TypingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<Message> Send(MessageRequest request, CancellationToken cancellationToken = default)
    {
        var entity = request.From.Entities.GetRequired<GithubInstallEntity>();

        if (entity.AccessToken.ExpiresAt >= DateTimeOffset.UtcNow.AddMinutes(-5))
        {
            entity.AccessToken = await AppClient.GitHubApps.CreateInstallationToken(entity.Install.Id);
            await Accounts.Update(request.From, cancellationToken);
        }

        var client = new Connection(new("TOS-Agent"), entity.AccessToken.Token);
        var query = new Mutation()
            .AddDiscussionComment(new AddDiscussionCommentInput()
            {
                DiscussionId = new ID(request.Chat.SourceId),
                ReplyToId = request is MessageReplyRequest reply ? new(reply.ReplyTo.SourceId) : null,
                Body = request.Text
            })
            .Select(res => new GithubDiscussionComment()
            {
                Id = res.Comment.Id,
                Url = res.Comment.Url,
                Body = res.Comment.Body,
                UpVotes = res.Comment.UpvoteCount
            })
            .Compile();

        var comment = await client.Run(
            query,
            cancellationToken: cancellationToken
        );

        var message = new Message()
        {
            ChatId = request.Chat.Id,
            AccountId = request.From.Id,
            SourceId = comment.Id.ToString(),
            SourceType = SourceType.Github,
            Url = comment.Url,
            Text = comment.Body,
            Entities = [
                new GithubDiscussionCommentEntity()
                {
                    Comment = comment
                }
            ]
        };

        return message;
    }

    public async Task<Message> Reply(MessageReplyRequest request, CancellationToken cancellationToken = default)
    {
        var message = await Send(request, cancellationToken);
        message.ReplyToId = request.ReplyTo.Id;
        return message;
    }
}