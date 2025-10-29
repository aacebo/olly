using Octokit.GraphQL;
using Octokit.GraphQL.Model;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public partial class GithubClient
{
    public override async Task<Message> Send(string text)
    {
        var client = await Github.GetGraphConnection(Install, CancellationToken);
        var query = new Mutation()
            .AddDiscussionComment(new AddDiscussionCommentInput()
            {
                DiscussionId = new ID(Chat.SourceId),
                Body = text
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
            cancellationToken: CancellationToken
        );

        var response = new Message()
        {
            ChatId = Chat.Id,
            // AccountId = Account.Id,
            ReplyToId = Message?.Id,
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

        return await Storage.Messages.Create(response, cancellationToken: CancellationToken);
    }

    public override async Task<Message> Send(string text, params Attachment[] attachments)
    {
        return await Send(text);
    }

    public override async Task<Message> Send(params Attachment[] attachments)
    {
        return await Send(string.Empty, attachments);
    }

    public override async Task<Message> SendUpdate(Guid id, string? text, params Attachment[] attachments)
    {
        var message = Message ?? throw new Exception("message not found");

        message.Text = text ?? message.Text;
        message.Attachments = attachments.Length > 0 ? attachments.ToList() : message.Attachments;

        var client = await Github.GetGraphConnection(Install, CancellationToken);
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
            cancellationToken: CancellationToken
        );

        message.Entities.Put(new GithubDiscussionCommentEntity()
        {
            Comment = comment
        });

        return await Storage.Messages.Update(message, cancellationToken: CancellationToken);
    }

    public override async Task<Message> SendReply(string text, params Attachment[] attachments)
    {
        return await Send(text, attachments);
    }
}