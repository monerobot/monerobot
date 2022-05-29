namespace MoneroBot.Daemon.Features;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneroBot.Database;
using MoneroBot.Fider;
using MoneroBot.Fider.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public record GetUnregisterdPosts();

public record UnregisteredPost(int PostNumber, bool IsExistingBounty);

public interface IGetUnregisteredPostsHandler
{
    public Task<List<UnregisteredPost>> HandleAsync(GetUnregisterdPosts command, CancellationToken token = default);
}

public class GetUnregisteredPostsHandler : IGetUnregisteredPostsHandler
{
    private readonly MoneroBotContext context;
    private readonly ILogger<GetUnregisteredPostsHandler> logger;
    private readonly IFiderApiClient fider;
    private readonly IGetDonationAddressCommentsHandler getDonationAddressComments;

    public GetUnregisteredPostsHandler(
        MoneroBotContext context,
        ILogger<GetUnregisteredPostsHandler> logger,
        IFiderApiClient fider,
        IGetDonationAddressCommentsHandler getDonationAddressComments)
    {
        this.context = context;
        this.logger = logger;
        this.fider = fider;
        this.getDonationAddressComments = getDonationAddressComments;
    }

    public async Task<List<UnregisteredPost>> HandleAsync(GetUnregisterdPosts command, CancellationToken token = default)
    {
        var result = new List<UnregisteredPost>();

        Post? latestPost;
        try
        {
            latestPost = await this.fider.GetLatestPostAsync(token);
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogError("Failed to fetch the latest post using the Fider API: {@fider_error}", exception);
            return result;
        }

        if (latestPost is null)
        {
            this.logger.LogInformation("No posts found in Fider");
            return result;
        }

        var importedPostNumbers = (await this.context.Bounties
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
                var post = await this.fider.GetPostAsync(number, token);
                var addresses = await this.getDonationAddressComments.HandleAsync(new GetDonationAddressComments(number), token);
                result.Add(new UnregisteredPost(PostNumber: number, IsExistingBounty: addresses.Any()));
            }
            catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
            {
                this.logger.LogInformation(
                    "When attempting to retrieve post #{post_number} the API returned a 404 Not Found response - the post was probably deleted",
                    number);
            }
            catch (HttpRequestException exception)
            {
                this.logger.LogError(
                    "An unhandled error occured when attempting to retrieve post #{post_number} from the API and determine if it already has donation address comments (we will try again later): {@exception}",
                    number,
                    exception);
            }
        }

        return result;
    }
}
