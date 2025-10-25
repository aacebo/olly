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
        var client = await Github.GetGraphConnection(request.Install, cancellationToken);
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
            AccountId = request.Account.Id,
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

    public async Task<Message> Update(MessageUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var message = request.Message;

        message.Text = request.Text ?? message.Text;
        message.Attachments = request.Attachments?.ToList() ?? message.Attachments;

        var client = await Github.GetGraphConnection(request.Install, cancellationToken);
        var query = new Mutation()
            .UpdateDiscussionComment(new UpdateDiscussionCommentInput()
            {
                CommentId = new(message.SourceId),
                Body = message.Text
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

        message.Entities.Put(new GithubDiscussionCommentEntity()
        {
            Comment = comment
        });

        return message;
    }

    public async Task<Message> Reply(MessageReplyRequest request, CancellationToken cancellationToken = default)
    {
        var message = await Send(request, cancellationToken);
        message.ReplyToId = request.ReplyTo.Id;
        return message;
    }
}