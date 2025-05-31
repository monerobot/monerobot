namespace MoneroBot.Daemon.Features;

using System.Net;
using Database;
using Fider;
using Fider.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetUnregisteredPosts;

public record UnregisteredPost(int PostNumber, bool HasExistingDonationComment);

public interface IGetUnregisteredPostsHandler
{
    public Task<List<UnregisteredPost>> HandleAsync(GetUnregisteredPosts command, CancellationToken token = default);
}

public class GetUnregisteredPostsHandler : IGetUnregisteredPostsHandler
{
    private readonly IDbContextFactory<MoneroBotContext> contextFactory;
    private readonly ILogger<GetUnregisteredPostsHandler> logger;
    private readonly IFiderApiClient fider;
    private readonly IGetDonationAddressCommentsHandler getDonationAddressComments;
    private readonly IApprovalCommentFeature awaitingApprovalCommentHandler;

    public GetUnregisteredPostsHandler(
        IDbContextFactory<MoneroBotContext> contextFactory,
        ILogger<GetUnregisteredPostsHandler> logger,
        IFiderApiClient fider,
        IGetDonationAddressCommentsHandler getDonationAddressComments,
        IApprovalCommentFeature awaitingApprovalCommentHandler)
    {
        this.contextFactory = contextFactory;
        this.logger = logger;
        this.fider = fider;
        this.getDonationAddressComments = getDonationAddressComments;
        this.awaitingApprovalCommentHandler = awaitingApprovalCommentHandler;
    }

    public async Task<List<UnregisteredPost>> HandleAsync(GetUnregisteredPosts command, CancellationToken token = default)
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(token);
        var result = new List<UnregisteredPost>();

        Post? latestPost;
        try
        {
            latestPost = await this.fider.GetLatestPostAsync(token);
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogError(exception, "Failed to fetch the latest post using the Fider API");
            return result;
        }

        if (latestPost is null)
        {
            this.logger.LogInformation("No posts found in Fider");
            return result;
        }

        var importedPostNumbers = (await context.Bounties
            .Select(b => b.PostNumber)
            .ToListAsync(token))
            .ToHashSet();
        for (var number = 1; number <= latestPost.Number; number++)
        {
            if (importedPostNumbers.Contains((uint)number))
            {
                continue;
            }

            try
            {
                /* we make this request to just check the post exists - if the requested failed with a 404 the
                 * `HTTPRequestException` would have been raised... yeah exception control flow but it is the
                 * way it's done for this specific scenario in C# land.
                 */
                var post = await this.fider.GetPostAsync(number, token);
                var addresses = await this.getDonationAddressComments.HandleAsync(new GetDonationAddressComments(number), token);
                var isExistingBounty = addresses is not [];
                result.Add(new UnregisteredPost(PostNumber: number, HasExistingDonationComment: isExistingBounty));
            }
            catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
            {
                this.logger.LogInformation(
                    "When attempting to retrieve post #{PostNumber} the API returned a 404 Not Found response - the post was probably deleted",
                    number);
            }
            catch (HttpRequestException exception)
            {
                this.logger.LogError(
                    exception,
                    "An unhandled error occured when attempting to retrieve post #{PostNumber} from the API and determine if it already has donation address comments (we will try again later)",
                    number);
            }
        }

        return result;
    }
}
