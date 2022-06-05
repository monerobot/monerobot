namespace MoneroBot.Daemon.Features;

using Database;
using Fider;
using Fider.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Db = Database.Entities;

public record RegisterExistingBounty(int PostNumber);

public interface IRegisterExistingBountyHandler
{
    public Task<int?> HandleAsync(RegisterExistingBounty command, CancellationToken token = default);
}

public class RegisterExistingBountyHandler : IRegisterExistingBountyHandler
{
    private readonly IDbContextFactory<MoneroBotContext> contextFactory;
    private readonly ILogger<RegisterExistingBountyHandler> logger;
    private readonly IGetDonationAddressCommentsHandler getDonationAddressComments;
    private readonly IFiderApiClient fider;

    public RegisterExistingBountyHandler(
        IDbContextFactory<MoneroBotContext> contextFactory,
        ILogger<RegisterExistingBountyHandler> logger,
        IGetDonationAddressCommentsHandler getDonationAddressComments,
        IFiderApiClient fider)
    {
        this.contextFactory = contextFactory;
        this.logger = logger;
        this.getDonationAddressComments = getDonationAddressComments;
        this.fider = fider;
    }

    public async Task<int?> HandleAsync(RegisterExistingBounty command, CancellationToken token = default)
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(token);

        Post? post;
        try
        {
            post = await this.fider.GetPostAsync(command.PostNumber, token);
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogError(
                exception,
                "Failed to fetch the post #{PostNumber} using the Fider API",
                command.PostNumber);
            return null;
        }

        var addressComments = 
            await this.getDonationAddressComments.HandleAsync(new GetDonationAddressComments(post.Number), token);

        if (addressComments.Any() is not true)
        {
            this.logger.LogError(
                "Attempted to import post #{PostNumber} as an existing bounty however there is no donation address comment(s) on the post which means it is not suitable " +
                "for importing as an existing bounty",
                post.Number);
            return null;
        }

        this.logger.LogInformation(
            "Post #{PostNumber} will be imported as an existing post",
            post.Number);

        var bounty = new Db.Bounty((uint)post.Number, post.Slug)
        {
            DonationAddresses = addressComments
                .Select(ac => new Db.DonationAddress(ac.Address, ac.CommentId))
                .ToList(),
        };
        context.Bounties.Add(bounty);

        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Failed to register post {@Post} due to a database error", post);
            return null;
        }
        
        this.logger.LogInformation("Successfully registered existing post {@Post}", post);
        return bounty.Id;
    }
}
