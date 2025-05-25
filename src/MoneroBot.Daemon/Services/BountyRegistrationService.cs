namespace MoneroBot.Daemon.Services;

using Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class BountyRegistrationService : IHostedService, IDisposable
{
    private readonly DaemonOptions options;
    private readonly ILogger<BountyRegistrationService> logger;
    private readonly IGetUnregisteredPostsHandler getUnregisteredPosts;
    private readonly IRegisterExistingBountyHandler registerExistingBounty;
    private readonly IRegisterNewBountyHandler registerNewBounty;
    private readonly IApprovalCommentFeature approvalCommentFeature;
    private CancellationTokenSource? cts;
    private Timer? timer;

    public BountyRegistrationService(
        IOptions<DaemonOptions> options,
        ILogger<BountyRegistrationService> logger,
        IGetUnregisteredPostsHandler getUnregisteredPosts,
        IRegisterExistingBountyHandler registerExistingBounty,
        IRegisterNewBountyHandler registerNewBounty,
        IApprovalCommentFeature awaitingApprovalComment)
    {
        this.options = options.Value;
        this.logger = logger;
        this.getUnregisteredPosts = getUnregisteredPosts;
        this.registerExistingBounty = registerExistingBounty;
        this.registerNewBounty = registerNewBounty;
        this.approvalCommentFeature = awaitingApprovalComment;
    }

    public Task StartAsync(CancellationToken token)
    {
        this.logger.LogInformation("The Bounty registration service which creates bounties for posts has started");

        this.cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        this.timer = new Timer(this.Tick, null, 0, Timeout.Infinite);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken token)
    {
        this.timer?.Change(Timeout.Infinite, Timeout.Infinite);
        this.cts?.Cancel();
        return Task.CompletedTask;
    }

    private async void Tick(object? state)
    {
        try
        {
            var token = this.cts?.Token ?? default;
            await this.PerformRegistrations(token);
        }
        catch (Exception exception)
        {
            this.logger.LogCritical(exception, "An unhandled exception occured whilst performing registrations");
        }
        finally
        {
            await Task.Delay(this.options.PollingInterval);
            this.timer?.Change(0, Timeout.Infinite);
        }
    }

    private async Task PerformRegistrations(CancellationToken token = default)
    {
        this.logger.LogTrace("Scanning for posts to register bounties for");

        var posts = await this.getUnregisteredPosts.HandleAsync(new GetUnregisteredPosts(), token);
        if (posts.Count is 0)
        {
            this.logger.LogTrace("There are no more posts to register at this time");
            return;
        }

        var existing = posts.Where(p => p.IsExistingBounty).ToList();
        var @new = posts.Except(existing).ToList();

        /* A bounty is considered 'existing' when the bot has already posted donation address(es) for it we
         * need to import any existing posts first because we want to avoid giving out an address which
         * is in use by a different existing post! And the only way we can check that is by first importing any
         * existing posts so that the information is in our database.
         *
         * Also, if a bounty already exists then it has to be considered already approved (otherwise how else
         * could it have gotten a donation address if we require approval first? well for bounties made prior to
         * the introduction of the 'requires approval' feature they will exist without approval but that is fine)
         * and so we don't touch any of the 'pending approval' comments.
         */
        foreach (var post in existing)
        {
            this.logger.LogInformation("Registering post #{PostNumber} as a bounty", post.PostNumber);
            var id = await this.registerExistingBounty.HandleAsync(new RegisterExistingBounty(post.PostNumber), token);
            if (id is not null)
            {
                this.logger.LogInformation(
                    "Successfully registered a bounty (id = {BountyId}) for existing post #{PostNumber}",
                    id,
                    post.PostNumber);
            }
            else
            {
                this.logger.LogError(
                    "Failed to register a bounty for post #{PostNumber} for an unknown reason - because " +
                    "this is an existing post the import process will halt and try again later. All existing posts " +
                    "must be imported before new ones can be processed so as to ensure that donation addresses are never " +
                    "reused. And we can only detect re-use if we know what donation addresses are already in use. ",
                    post.PostNumber);
                return;
            }
        }

        foreach (var post in @new)
        {
            var postApprovalStateResult = await this.approvalCommentFeature.GetPostApprovalState(post.PostNumber, token);
            if (postApprovalStateResult.IsErr(out var postApprovalStateErr, out var postApprovalState))
            {
                this.logger.LogError(
                    "Failed to determine if the bounty for post #{PostNumber} has been approved or rejected, this means we cannot proceed " +
                    "with registering the bounty as we don't want to allow donations to posts that haven't been approved or have been rejected. " +
                    "We will try again on the next tick. {@Error}",
                    post.PostNumber,
                    postApprovalStateErr);
                continue;
            }

            var approvalCommentResult = await this.approvalCommentFeature.GetApprovalCommentAsync(post.PostNumber, token);
            if (postApprovalState is PostAppovalState.Approved)
            {
                if (approvalCommentResult.IsErr(out var approvalCommentErr, out var approvalComment))
                {
                    this.logger.LogError(
                        "An approved bounty for post #{PostNumber} was encountered but we were not able to retrieve the approval status comment " +
                        "due to an error. The approval post is a value add so best to just move on rather hold up posting a donation address. {@Error}",
                        post.PostNumber,
                        approvalCommentErr);
                }
                else if (approvalComment is null)
                {
                    this.logger.LogWarning(
                        "An approved bounty for post #{PostNumber} was encountered which has no approval status comment, this would be because it was posted at " +
                        "the same time the approval status feature was deployed or if the comments were edited outside of this bot. This isn't " +
                        "a serious error because well we'll just post a donation address. The approval post is a value add, and this code path should be " +
                        "extreemly rare. Best to just move on rather than try to retroactively add an 'approved' comment.",
                        post.PostNumber);
                }
                else if (approvalComment.State is not ApprovalCommentState.AwaitingApproval)
                {
                    this.logger.LogInformation("Updating approval comment for post #{PostNumber} to the approved state", post.PostNumber);
                    _ = await this.approvalCommentFeature.UpdateAwaitingApprovalCommentToApprovedStateAsync(post.PostNumber, token);
                }

                this.logger.LogInformation("Importing post #{PostNumber} as a new bounty", post.PostNumber);
                var id = await this.registerNewBounty.HandleAsync(new RegisterNewBounty(post.PostNumber, this.options.WalletAccountIndex), token);
                if (id is not null)
                {
                    this.logger.LogInformation(
                        "Successfully registered a bounty (id = {BountyId}) for new post #{PostNumber}",
                        id,
                        post.PostNumber);
                }
                else
                {
                    this.logger.LogError(
                        "Failed to register a bounty for post #{PostNumber} for an unknown reason",
                        post.PostNumber);
                    return;
                }
            }
            else if (postApprovalState is PostAppovalState.None)
            {
                if (approvalCommentResult.IsErr(out var approvalCommentErr, out var approvalComment))
                {
                    this.logger.LogError(
                        "An unregistered bounty for post #{PostNumber} was found but we couldn't determine if the approval comment was already posted due to an unknown error. " +
                        "We don't want to proceed with trying to create an approval comment because it could already have one! We'd rather be conservative and " +
                        "not spam the bounty with awaiting approval comments. {@Error}",
                        post.PostNumber,
                        approvalCommentErr);
                }
                else if (approvalComment is null)
                {
                    this.logger.LogInformation("Posting an awaiting approval comment for post #{PostNumber}", post.PostNumber);
                    _ = await this.approvalCommentFeature.PostAwaitingApprovalCommentAsync(post.PostNumber, token);
                }
            }
        }
    }

    /// <inheritdoc />
#pragma warning disable SA1202 // Elements should be ordered by access
    public void Dispose()
#pragma warning restore SA1202 // Elements should be ordered by access
    {
        this.timer?.Dispose();
    }
}
