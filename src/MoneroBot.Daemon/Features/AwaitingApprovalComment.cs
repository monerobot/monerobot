namespace MoneroBot.Daemon.Features;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneroBot.Fider;
using MoneroBot.Fider.Models;

public record GetApprovalCommentResult(ApprovalComment? Comment)
{
    [MemberNotNullWhen(true, nameof(Comment))]
    public bool HasComment => Comment is not null;
}

public record ApprovalComment(int CommentId, bool IsInApprovedState);

public record UpdateApprovalCommentResult(bool WasPresent, bool WasUpdated, ApprovalComment? Comment);

public interface IApprovalCommentFeature
{
    public Task<bool?> IsPostApprovedAsync(int postNumber, CancellationToken token);

    public Task<Option<GetApprovalCommentResult>> GetApprovalCommentAsync(int postNumber, CancellationToken token);

    public Task<Option<ApprovalComment>> PostAwaitingApprovalCommentAsync(int postNumber, CancellationToken token = default);

    public Task<Option<UpdateApprovalCommentResult>> UpdateAwaitingApprovalCommentToApprovedStateAsync(int postNumber, CancellationToken token = default);
}

internal static partial class ApprovalCommentTexts
{
    public const string AwaitingApprovalMessage = @"I've detected this bounty (don't worry!) but am waiting for an admin to adorn it with an `approved` tag. Once I see that the bounty has been approved I will post a donation address!";

    public const string HasBeenApprovedMessage = @"This bounty has been approved (congratulations!), take a look below for a comment from myself to find the donation address details!";

    public const string ApprovedTag = "approved";
}

public class AwaitingCommentFeature(IFiderApiClient fider, ILogger<AwaitingCommentFeature> logger, IOptions<DaemonOptions> options) : IApprovalCommentFeature
{
    public async Task<bool?> IsPostApprovedAsync(int postNumber, CancellationToken token)
    {
        try
        {
            var post = await fider.GetPostAsync(postNumber, token);
            return post.Tags.Contains(ApprovalCommentTexts.ApprovedTag, StringComparer.OrdinalIgnoreCase);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Failed to fetch the post #{PostNumber} and so the approval status could not be determined", postNumber);
            return default;
        }
    }

    public async Task<Option<GetApprovalCommentResult>> GetApprovalCommentAsync(int postNumber, CancellationToken token = default)
    {
        List<Comment> comments;
        try
        {
            var post = await fider.GetPostAsync(postNumber, token);
            comments = await fider.ListCommentsAsync(post.Number, post.CommentsCount, token);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Failed to fetch the comments for post #{PostNumber} and so the approval comment could not be searched for", postNumber);
            return Option.None<GetApprovalCommentResult>();
        }

        foreach (var comment in comments)
        {
            if (comment.User.Id != options.Value.FiderMoneroBotUserId)
            {
                continue;
            }

            var matchesAwaitingApprovalMessage = comment.Content.Contains(ApprovalCommentTexts.AwaitingApprovalMessage, StringComparison.OrdinalIgnoreCase);
            var matchesHasBeenApprovedMessage = comment.Content.Contains(ApprovalCommentTexts.HasBeenApprovedMessage, StringComparison.OrdinalIgnoreCase);
            var isApprovalComment = matchesAwaitingApprovalMessage || matchesHasBeenApprovedMessage;

            if (isApprovalComment is false)
            {
                continue;
            }

            return Option.For(new GetApprovalCommentResult(new ApprovalComment(comment.Id, IsInApprovedState: matchesHasBeenApprovedMessage)));
        }

        return Option.For(new GetApprovalCommentResult(Comment: null));
    }


    public async Task<Option<ApprovalComment>> PostAwaitingApprovalCommentAsync(int postNumber, CancellationToken token = default)
    {
        try
        {
            var content = $"{ApprovalCommentTexts.AwaitingApprovalMessage}\n";
            var attachment = new ImageUpload(
                BlobKey: $"post_{postNumber}_awaiting_approval_banner",
                Upload: new ImageUploadData(
                    FileName: "awaiting-approval-banner",
                    ContentType: "image/png",
                    Content: await File.ReadAllBytesAsync("./Assets/awaiting-approval-banner.png", token)),
                Remove: false);
            var commentId = await fider.PostCommentAsync(
                postNumber,
                content,
                [attachment],
                token);
            logger.LogInformation(
                "Successfully created an approval comment ({CommentId}) {@Comment} for post #{PostNumber}",
                commentId,
                new { Content = content },
                postNumber);

            return Option.For((await GetApprovalCommentAsync(postNumber, token)).Unwrap()?.Comment);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Failed to create an approval comment for post #{PostNumber}", postNumber);
            return Option.None<ApprovalComment>();
        }
    }

    public async Task<Option<UpdateApprovalCommentResult>> UpdateAwaitingApprovalCommentToApprovedStateAsync(int postNumber, CancellationToken token = default)
    {
        var comment = (await GetApprovalCommentAsync(postNumber, token)).Unwrap()?.Comment;

        if (comment is null)
        {
            logger.LogInformation(
                "The approval comment for post #{PostNumber} could not be found either because of an error or because doesn't exist, and so it could not be transitioned to the approved state.",
                postNumber);
            return Option.For(new UpdateApprovalCommentResult(WasPresent: false, WasUpdated: false, Comment: null));
        }

        if (comment.IsInApprovedState)
        {
            logger.LogInformation(
                "The approval comment for post #{PostNumber} is already in the approved state so there is nothing to be updated",
                postNumber);
            return Option.For(new UpdateApprovalCommentResult(WasPresent: true, WasUpdated: false, Comment: comment));
        }

        try
        {
            var content = $"{ApprovalCommentTexts.HasBeenApprovedMessage}\n";
            var attachment = new ImageUpload(
                BlobKey: $"post_{postNumber}_approved_banner",
                Upload: new ImageUploadData(
                    FileName: "approved-banner",
                    ContentType: "image/png",
                    Content: await File.ReadAllBytesAsync("./Assets/approved-banner.png", token)),
                Remove: false);
            await fider.UpdateCommentAsync(
                postNumber,
                comment.CommentId,
                content,
                [attachment],
                token);
            logger.LogInformation(
                "Successfully updated the approval comment ({CommentId}) {@Comment} for post #{PostNumber} to the approved state",
                comment.CommentId,
                new { Content = content },
                postNumber);

            comment = (await GetApprovalCommentAsync(postNumber, token)).Unwrap()?.Comment;
            return Option.For(new UpdateApprovalCommentResult(WasPresent: true, WasUpdated: true, Comment: comment));
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Failed to update approval comment ({@Comment}) for post #{PostNumber} to the approved state", comment, postNumber);
            return Option.For(new UpdateApprovalCommentResult(WasPresent: true, WasUpdated: false, Comment: comment));
        }
    }
}