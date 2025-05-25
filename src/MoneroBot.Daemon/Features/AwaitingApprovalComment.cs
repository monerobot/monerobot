namespace MoneroBot.Daemon.Features;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneroBot.Daemon.Errors;
using MoneroBot.Fider;
using MoneroBot.Fider.Models;

public enum PostAppovalState
{
    None,
    Approved,
    Rejected
}

public enum ApprovalCommentState
{
    None,
    AwaitingApproval,
    Approved,
    Rejected
}

public record ApprovalComment(int CommentId, ApprovalCommentState State);

public record ApprovalCommentUpdateResult(bool WasPresent, bool WasUpdated, ApprovalComment? Comment);

internal static partial class ApprovalCommentTexts
{
    public const string AwaitingApprovalMessage = @"I've detected this bounty (don't worry!) but am waiting for an admin to adorn it with an `approved` tag. Once I see that the bounty has been approved I will post a donation address!";

    public const string HasBeenApprovedMessage = @"This bounty has been approved (congratulations!), take a look below for a comment from myself to find the donation address details!";

    public const string HasBeenRejectedMessage = @"This bounty has been rejected (sorry!), take a look below for a comment from an admin that'll explain why it was rejected.";

    public const string ApprovedTag = "approved";

    public const string RejectedTag = "rejected";
}

public interface IApprovalCommentFeature
{
    public Task<Result<PostAppovalState, FiderError>> GetPostApprovalState(int postNumber, CancellationToken token);

    public Task<Result<ApprovalComment?, FiderError>> GetApprovalCommentAsync(int postNumber, CancellationToken token);

    public Task<Result<ApprovalComment, FiderError>> PostAwaitingApprovalCommentAsync(int postNumber, CancellationToken token = default);

    public Task<Result<ApprovalCommentUpdateResult, FiderError>> UpdateAwaitingApprovalCommentToApprovedStateAsync(int postNumber, CancellationToken token = default);

    public Task<Result<ApprovalCommentUpdateResult, FiderError>> UpdateAwaitingApprovalCommentToRejectedStateAsync(int postNumber, CancellationToken token = default);
}


public class AwaitingCommentFeature(IFiderApiClient fider, ILogger<AwaitingCommentFeature> logger, IOptions<DaemonOptions> options) : IApprovalCommentFeature
{
    public async Task<Result<PostAppovalState, FiderError>> GetPostApprovalState(int postNumber, CancellationToken token)
    {
        try
        {
            var post = await fider.GetPostAsync(postNumber, token);

            var state = PostAppovalState.None;
            if (post.Tags.Contains(ApprovalCommentTexts.ApprovedTag, StringComparer.OrdinalIgnoreCase)) state = PostAppovalState.Approved;
            if (post.Tags.Contains(ApprovalCommentTexts.RejectedTag, StringComparer.OrdinalIgnoreCase)) state = PostAppovalState.Rejected;

            return Result<PostAppovalState, FiderError>.Ok(state);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Failed to fetch the post #{PostNumber} and so the approval status could not be determined", postNumber);
            return Result<PostAppovalState, FiderError>.Err(new FiderError.ApiError(exception));
        }
    }

    public async Task<Result<ApprovalComment?, FiderError>> GetApprovalCommentAsync(int postNumber, CancellationToken token = default)
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
            return Result<ApprovalComment?, FiderError>.Err(new FiderError.ApiError(exception));
        }

        foreach (var comment in comments)
        {
            if (comment.User.Id != options.Value.FiderMoneroBotUserId)
            {
                continue;
            }

            var matchesAwaitingApprovalMessage = comment.Content.Contains(ApprovalCommentTexts.AwaitingApprovalMessage, StringComparison.OrdinalIgnoreCase);
            var matchesHasBeenApprovedMessage = comment.Content.Contains(ApprovalCommentTexts.HasBeenApprovedMessage, StringComparison.OrdinalIgnoreCase);
            var matchesHasBeenRejectedMessage = comment.Content.Contains(ApprovalCommentTexts.HasBeenRejectedMessage, StringComparison.OrdinalIgnoreCase);
            var isApprovalComment = matchesAwaitingApprovalMessage || matchesHasBeenApprovedMessage || matchesHasBeenRejectedMessage;

            if (isApprovalComment is false)
            {
                continue;
            }

            var state = matchesAwaitingApprovalMessage
                ? ApprovalCommentState.AwaitingApproval
                : matchesHasBeenApprovedMessage
                    ? ApprovalCommentState.Approved
                    : matchesHasBeenRejectedMessage
                        ? ApprovalCommentState.Rejected
                        : ApprovalCommentState.None;

            return Result<ApprovalComment?, FiderError>.Ok(new ApprovalComment(comment.Id, state));
        }

        return Result<ApprovalComment?, FiderError>.Ok(default);
    }


    public async Task<Result<ApprovalComment, FiderError>> PostAwaitingApprovalCommentAsync(int postNumber, CancellationToken token = default)
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

            var result = await GetApprovalCommentAsync(postNumber, token);
            return result
                .ErrIf(
                    comment => comment is null,
                    new FiderError.UnexpectedResult("The call to create a comment succeeded but the comment could not then be found"))
                .Map(comment => comment!);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Failed to create an approval comment for post #{PostNumber}", postNumber);
            return Result<ApprovalComment, FiderError>.Err(new FiderError.ApiError(exception));
        }
    }

    public async Task<Result<ApprovalCommentUpdateResult, FiderError>> UpdateAwaitingApprovalCommentToApprovedStateAsync(int postNumber, CancellationToken token = default)
    {
        var commentResult = await GetApprovalCommentAsync(postNumber, token);

        if (commentResult.IsErr(out var error, out var comment))
        {
            logger.LogInformation(
                "The approval comment for post #{PostNumber} could not be found either because of an error or because doesn't exist, and so it could not be transitioned to the approved state.",
                postNumber);
            return Result<ApprovalCommentUpdateResult, FiderError>.Err(error);
        }

        if (comment.State is ApprovalCommentState.Approved)
        {
            logger.LogInformation(
                "The approval comment for post #{PostNumber} is already in the approved state so there is nothing to be updated",
                postNumber);
            return Result<ApprovalCommentUpdateResult, FiderError>.Ok(new ApprovalCommentUpdateResult(WasPresent: true, WasUpdated: false, Comment: comment));
        }

        try
        {
            var content = $"{ApprovalCommentTexts.HasBeenApprovedMessage}\n";
            await fider.UpdateCommentAsync(
                postNumber,
                comment.CommentId,
                content: content,
                attachments:
                [
                     new ImageUpload(
                        BlobKey: $"post_${postNumber}_awaiting_approval_banner",
                        Upload: null,
                        Remove: true),
                     new ImageUpload(
                        BlobKey: $"post_{postNumber}_approved_banner",
                        Upload: new ImageUploadData(
                            FileName: "approved-banner",
                            ContentType: "image/png",
                            Content: await File.ReadAllBytesAsync("./Assets/approved-banner.png", token)),
                        Remove: false)
                ],
                token);
            logger.LogInformation(
                "Successfully updated the approval comment ({CommentId}) {@Comment} for post #{PostNumber} to the approved state",
                comment.CommentId,
                new { Content = content },
                postNumber);

            comment = (await GetApprovalCommentAsync(postNumber, token)).Unwrap();
            return Result<ApprovalCommentUpdateResult, FiderError>.Ok(new ApprovalCommentUpdateResult(WasPresent: true, WasUpdated: true, Comment: comment));
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Failed to update approval comment ({@Comment}) for post #{PostNumber} to the approved state", comment, postNumber);
            return Result<ApprovalCommentUpdateResult, FiderError>.Ok(new ApprovalCommentUpdateResult(WasPresent: true, WasUpdated: false, Comment: comment));
        }
    }

    public async Task<Result<ApprovalCommentUpdateResult, FiderError>> UpdateAwaitingApprovalCommentToRejectedStateAsync(int postNumber, CancellationToken token = default)
    {
        var commentResult = await GetApprovalCommentAsync(postNumber, token);

        if (commentResult.IsErr(out var error, out var comment))
        {
            logger.LogInformation(
                "The approval comment for post #{PostNumber} could not be found either because of an error or because doesn't exist, and so it could not be transitioned to the rejected state.",
                postNumber);
            return Result<ApprovalCommentUpdateResult, FiderError>.Err(error);
        }

        if (comment.State is ApprovalCommentState.Rejected)
        {
            logger.LogInformation(
                "The approval comment for post #{PostNumber} is already in the rejected state so there is nothing to be updated",
                postNumber);
            return Result<ApprovalCommentUpdateResult, FiderError>.Ok(new ApprovalCommentUpdateResult(WasPresent: true, WasUpdated: false, Comment: comment));
        }

        try
        {
            var content = $"{ApprovalCommentTexts.HasBeenRejectedMessage}\n";
            await fider.UpdateCommentAsync(
                postNumber,
                comment.CommentId,
                content: content,
                attachments:
                [
                    new ImageUpload(
                        BlobKey: $"post_${postNumber}_awaiting_approval_banner",
                        Upload: null,
                        Remove: true),
                     new ImageUpload(
                        BlobKey: $"post_${postNumber}_approved_banner",
                        Upload: null,
                        Remove: true),
                     new ImageUpload(
                        BlobKey: $"post_{postNumber}_rejected_banner",
                        Upload: new ImageUploadData(
                            FileName: "rejected-banner",
                            ContentType: "image/png",
                            Content: await File.ReadAllBytesAsync("./Assets/rejected-banner.png", token)),
                        Remove: false)
                ],
                token);
            logger.LogInformation(
                "Successfully updated the approval comment ({CommentId}) {@Comment} for post #{PostNumber} to the rejected state",
                comment.CommentId,
                new { Content = content },
                postNumber);

            comment = (await GetApprovalCommentAsync(postNumber, token)).Unwrap();
            return Result<ApprovalCommentUpdateResult, FiderError>.Ok(new ApprovalCommentUpdateResult(WasPresent: true, WasUpdated: true, Comment: comment));
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Failed to update approval comment ({@Comment}) for post #{PostNumber} to the rejected state", comment, postNumber);
            return Result<ApprovalCommentUpdateResult, FiderError>.Err(new FiderError.ApiError(exception));
        }
    }
}