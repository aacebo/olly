using Octokit.GraphQL;
using Octokit.GraphQL.Model;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Drivers.Models;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public partial class GithubDriver
{
    public Task Typing(TypingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<Message> Send(MessageRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Install.ExpiresAt is null || request.Install.ExpiresAt >= DateTimeOffset.UtcNow.AddMinutes(-5))
        {
            var accessToken = await AppClient.GitHubApps.CreateInstallationToken(long.Parse(request.Install.SourceId));
            request.Install.AccessToken = accessToken.Token;
            request.Install.ExpiresAt = accessToken.ExpiresAt;
            await Installs.Update(request.Install, cancellationToken);
        }

        var client = new Connection(new("TOS-Agent"), request.Install.AccessToken);
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