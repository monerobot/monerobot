namespace MoneroBot.Daemon.Features;

using Microsoft.Extensions.Logging;
using MoneroBot.Database;
using Db = MoneroBot.Database.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoneroBot.Fider;
using MoneroBot.Fider.Models;

public record RegisterExistingBounty(int PostNumber);

public interface IRegisterExistingBountyHandler
{
    public Task<int?> HandleAsync(RegisterExistingBounty command, CancellationToken token = default);
}

public class RegisterExistingBountyHandler : IRegisterExistingBountyHandler
{
    private readonly MoneroBotContext context;
    private readonly ILogger<RegisterExistingBountyHandler> logger;
    private readonly IGetDonationAddressCommentsHandler getDonationAddressComments;
    private readonly IGetDonationCommentsHandler getDonationComments;
    private readonly IGetAddressIndexHander getAddressIndex;
    private readonly IGetIncomingTransfersHandler getIncomingTransfers;
    private readonly IReconcileDonationsWithTransfers reconcileDonationsWithTransfers;
    private readonly IFiderApiClient fider;

    public RegisterExistingBountyHandler(
        MoneroBotContext context,
        ILogger<RegisterExistingBountyHandler> logger,
        IGetDonationAddressCommentsHandler getDonationAddressComments,
        IGetDonationCommentsHandler getDonationComments,
        IGetAddressIndexHander getAddressIndex,
        IGetIncomingTransfersHandler getIncomingTransfers,
        IReconcileDonationsWithTransfers reconcileDonationsWithTransfers,
        IFiderApiClient fider)
    {
        this.context = context;
        this.logger = logger;
        this.getDonationAddressComments = getDonationAddressComments;
        this.getDonationComments = getDonationComments;
        this.getAddressIndex = getAddressIndex;
        this.getIncomingTransfers = getIncomingTransfers;
        this.reconcileDonationsWithTransfers = reconcileDonationsWithTransfers;
        this.fider = fider;
    }

    public async Task<int?> HandleAsync(RegisterExistingBounty command, CancellationToken token = default)
    {
        Post? post;
        try
        {
            post = await this.fider.GetPostAsync(command.PostNumber, token);
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogError(
                "Failed to fetch the post #{post_number} using the Fider API: {@fider_error}",
                command.PostNumber,
                exception);
            return null;
        }

        if (post is null)
        {
            this.logger.LogInformation(
                "Failed to fetch the post #{post_number} using the Fider API - the API returned a response but it was empty",
                command.PostNumber);
            return null;
        }

        var addressComments = await this.getDonationAddressComments.HandleAsync(new GetDonationAddressComments(post.Number), token);
        var donationComments = await this.getDonationComments.HandleAsync(new GetDonationComments(post.Number), token);
        this.logger.LogInformation(
            "Registering post #{post_number} using the existing donation address(es) {@addresses} and donation(s) {@donations}",
            post.Number,
            addressComments,
            donationComments);

        if (addressComments.Any() is not true)
        {
            this.logger.LogError(
                "Attempted to import post #{post_number} as an existing bounty however there is no donation address comment(s) on the post which means it is not suitable" +
                "for importing as an existing bounty.",
                post.Number);
            return null;
        }

        var transfers = new List<Transfer>();
        foreach (var address in addressComments)
        {
            var index = await this.getAddressIndex.HandleAsync(new GetAddressIndex(address.Address), token);
            if (index is null)
            {
                this.logger.LogCritical(
                    "Failed to determine the index for {address} and therefore cannot scan for incoming transfers",
                    address);
                continue;
            }

            var destination = new Destination(address.Address, index.Major, index.Minor);
            var incomingTransfers = await this.getIncomingTransfers.HandleAsync(new GetIncomingTransfers(destination), token);
            if (incomingTransfers is null)
            {
                this.logger.LogCritical(
                    "Failed to fetch the incoming transfers for {address} and therefore cannot attempt to reconcile donation comment(s) with transfers.",
                    destination.Address);
                continue;
            }

            transfers.AddRange(incomingTransfers.Transfers);
        }

        var donations = donationComments
            .Select(c => new Donation(c.CommentId, c.Amount, c.TotalAmount))
            .ToList();
        var reconciledDonations = await this.reconcileDonationsWithTransfers.HandleAsync(new ReconcileDonationsWithTransfers(donations, transfers, default), token);
        this.logger.LogInformation(
            "Post #{post_number} will be imported as an existing post, using the following reconciled donations: {@reconciled_donations}. These donations" +
            " were reconciled based upon scanning these addresses {@addresses} for transfers which matched the existing donation comments {@donation_comments}",
            post.Number,
            reconciledDonations,
            addressComments,
            donationComments);

        var bounty = await this.CreateBounty(
            post,
            addressComments,
            donationComments,
            reconciledDonations.Donations);
        return bounty.Id;
    }

    private async Task<Db.Bounty> CreateBounty(
        Post post,
        List<DonationAddressComment> donationAddressComments,
        List<DonationComment> donationComments,
        List<ReconciledDonation> reconciledDonations)
    {
        var bounty = new Db.Bounty((uint)post.Number, post.Slug)
        {
            DonationAddresses = new List<Db.DonationAddress>(),
        };

        foreach (var addressComment in donationAddressComments)
        {
            var address = new Db.DonationAddress(bounty, addressComment.Address)
            {
                Address = addressComment.Address,
                Comment = new Db.Comment(addressComment.CommentId, addressComment.Content),
                Donations = new List<Db.Donation>(),
            };

            var addressReconciledDonations = reconciledDonations
                .Where(rc => rc.Transfer.Destination.Address == address.Address);
            
            foreach (var (donationCommentId, transfer) in addressReconciledDonations)
            {
                var donationComment = donationComments.Single(dc => dc.CommentId == donationCommentId);
                var donation = new Db.Donation(address)
                {
                    Comment = new Db.Comment(donationComment.CommentId, donationComment.Content),
                    DonationEnotes = new List<Db.DonationEnote>(),
                };

                foreach (var enote in transfer.Enotes)
                {
                    var dbEnote = new Db.Enote(address: transfer.Destination.Address, pubKey: enote.PubKey, txHash: transfer.TxHash)
                    {
                        BlockHeight = transfer.BlockHeight,
                        Amount = enote.Amount,
                        IsSpent = enote.IsSpent,
                        IsUnlocked = enote.IsUnlocked,
                    };

                    donation.DonationEnotes.Add(new Db.DonationEnote(donation, dbEnote));
                }

                address.Donations.Add(donation);
            }

            bounty.DonationAddresses.Add(address);
        }

        this.context.Bounties.Add(bounty);
        await this.context.SaveChangesAsync();
        return bounty;
    }
}
