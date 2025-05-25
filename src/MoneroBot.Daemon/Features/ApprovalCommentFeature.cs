namespace MoneroBot.Daemon.Features;

using System.Diagnostics;
using System.Net;
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

public record ApprovalComment(int CommentId, string[] Attachments, ApprovalCommentState State);

public record ApprovalCommentUpdate(bool WasUpdated, ApprovalComment ApprovalComment);

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

    public Task<Result<ApprovalCommentUpdate, FiderError>> UpsertApprovalComment(int postNumber, ApprovalCommentState state, CancellationToken token = default);
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
        Post post;
        Comment[] comments;
        try
        {
            post = await fider.GetPostAsync(postNumber, token);
            comments = await fider.ListCommentsAsync(post.Number, post.CommentsCount, token);
        }
        catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
        {
            logger.LogInformation(
                exception,
                "Attemptd to retrieve the comments for post #{PostNumber} but that post no longer exists " +
                "and so the approval comment will be treated as not existing.",
                postNumber);
            return Result<ApprovalComment?, FiderError>.Ok(null);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Attemptd to retrieve the comments for post #{PostNumber} but that post no longer exists " +
                "and so a list of donation comments could not be determined.",
                postNumber);
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

            return Result<ApprovalComment?, FiderError>.Ok(new ApprovalComment(comment.Id, comment.Attachments ?? [], state));
        }

        return Result<ApprovalComment?, FiderError>.Ok(default);
    }

    public async Task<Result<ApprovalCommentUpdate, FiderError>> UpsertApprovalComment(int postNumber, ApprovalCommentState state, CancellationToken token = default)
    {
        var commentResult = await GetApprovalCommentAsync(postNumber, token);

        if (commentResult.IsErr(out var commentError, out var comment))
        {
            logger.LogError(
                "Failed to retrieve the approval comment for post #{PostNumber} due to an error and so we could not determine if " +
                "we need to create a post or update an existing one to make an approval comment in the `{State}` exist " +
                "for the aforementioned post. {@Error}",
                postNumber,
                state,
                commentError);
            return Result<ApprovalCommentUpdate, FiderError>.Err(commentError);
        }

        if (state is ApprovalCommentState.None)
        {
            logger.LogWarning(
                "An attempt to upsert the approval comment for post #{PostNumber} into the `None` state was made, this is not allowed.",
                postNumber);
            return Result<ApprovalCommentUpdate, FiderError>.Ok(new ApprovalCommentUpdate(WasUpdated: false, ApprovalComment: comment));
        }

        var content = state switch
        {
            ApprovalCommentState.None => throw new UnreachableException("The `None` case should have been handled by code above"),
            ApprovalCommentState.AwaitingApproval => $"{ApprovalCommentTexts.AwaitingApprovalMessage}\n",
            ApprovalCommentState.Approved => $"{ApprovalCommentTexts.HasBeenApprovedMessage}\n",
            ApprovalCommentState.Rejected => $"{ApprovalCommentTexts.HasBeenRejectedMessage}\n",
        };

        var banner = state switch
        {
            ApprovalCommentState.None => throw new UnreachableException("The `None` case should have been handled by code above"),
            ApprovalCommentState.AwaitingApproval =>
                ImageAttachment.Addition(
                    blobKey: $"post_${postNumber}_awaiting_approval_banner",
                    upload: new ImageUploadData(
                        FileName: "awaiting_approval_banner",
                        ContentType: "image/png",
                        Content: await File.ReadAllBytesAsync("./Assets/awaiting-approval-banner.png", token))),
            ApprovalCommentState.Rejected =>
                ImageAttachment.Addition(
                    blobKey: $"post_{postNumber}_rejected_banner",
                    upload: new ImageUploadData(
                        FileName: "rejected_banner",
                        ContentType: "image/png",
                        Content: await File.ReadAllBytesAsync("./Assets/rejected-banner.png", token))),
            ApprovalCommentState.Approved =>
                ImageAttachment.Addition(
                    blobKey: $"post_${postNumber}_approved_banner",
                    upload: new ImageUploadData(
                        FileName: "approved_banner",
                        ContentType: "image/png",
                        Content: await File.ReadAllBytesAsync("./Assets/approved-banner.png", token)))
        };

        ImageAttachment[] attachments =
        [
            .. comment?.Attachments.Select(ImageAttachment.Removal) ?? [],
            banner
        ];

        try
        {
            if (comment is { CommentId: var commentId })
            {
                await fider.UpdateCommentAsync(
                    postNumber,
                    commentId,
                    content,
                    attachments,
                    token);
                logger.LogInformation(
                    "Successfully updated the approval comment ({CommentId}) {@Comment} for post #{PostNumber} to state `{State}`",
                    commentId,
                    state,
                    new { Content = content },
                    postNumber);
            }
            else
            {
                commentId = await fider.PostCommentAsync(
                    postNumber,
                    content,
                    attachments,
                    token);
                logger.LogInformation(
                    "Successfully created an approval comment ({CommentId}) in state `{State}` {@Comment} for post #{PostNumber}",
                    commentId,
                    state,
                    new { Content = content },
                    postNumber);
            }

            var result = await GetApprovalCommentAsync(postNumber, token);
            return result
                .ErrIf(
                    comment => comment is null,
                    new FiderError.UnexpectedResult("The call to create a comment succeeded but the comment could not then be found"))
                .Map(comment => new ApprovalCommentUpdate(WasUpdated: true, ApprovalComment: comment!));
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Failed to upsert the approval comment for post #{PostNumber} into state `{State}`",
                postNumber,
                state);
            return Result<ApprovalCommentUpdate, FiderError>.Err(new FiderError.ApiError(exception));
        }
    }
}