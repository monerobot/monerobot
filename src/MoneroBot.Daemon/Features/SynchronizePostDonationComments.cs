namespace MoneroBot.Daemon.Features;

using System.Globalization;
using System.Text;
using Database;
using Fider;
using Fider.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Db = Database.Entities;

public record SynchronizePostDonationComments(uint PostNumber);

public interface ISynchronizePostDonationCommentsHandler
{
    public Task HandleAsync(SynchronizePostDonationComments command, CancellationToken token = default);
}

public class SynchronizePostDonationCommentsHandler : ISynchronizePostDonationCommentsHandler
{
    private readonly ILogger<SynchronizePostDonationCommentsHandler> logger;
    private readonly IDbContextFactory<MoneroBotContext> contextFactory;
    private readonly IFiderApiClient fider;
    private readonly IGetAddressIndexHandler getAddressIndex;
    private readonly IGetIncomingTransfersHandler getIncomingTransfers;
    private readonly IGetDonationCommentsHandler getDonationComments;

    public SynchronizePostDonationCommentsHandler(
        ILogger<SynchronizePostDonationCommentsHandler> logger,
        IDbContextFactory<MoneroBotContext> contextFactory,
        IFiderApiClient fider,
        IGetAddressIndexHandler getAddressIndex,
        IGetIncomingTransfersHandler getIncomingTransfers,
        IGetDonationCommentsHandler getDonationComments)
    {
        this.logger = logger;
        this.contextFactory = contextFactory;
        this.fider = fider;
        this.getAddressIndex = getAddressIndex;
        this.getIncomingTransfers = getIncomingTransfers;
        this.getDonationComments = getDonationComments;
    }

    private static string FormatDonationAsComment(Donation donation, IEnumerable<Donation> all)
    {
        static string FormatAtomicAmount(CultureInfo culture, ulong amount)
        {
            const decimal ATOMIC_TO_MONERO_SCALER = 1e-12m;
            var separator = culture.NumberFormat.NumberDecimalSeparator;
            var moneros = amount * ATOMIC_TO_MONERO_SCALER;
            /* the ##... section is the exact length required to represent a single piconero, basically you
             * get the decimal representation of 1e-12 and then turn all the digits into #s.
             */
            return moneros.ToString(culture).Contains(separator) ? $"{moneros:0.############}" : $"{moneros:N0}";
        }

        var total = all
            .Where(d => d.Order <= donation.Order)
            .Aggregate(0ul, (t, d) => t + d.Amount);
        
        var sb = new StringBuilder();
        sb.Append($"Bounty increased by {FormatAtomicAmount(CultureInfo.InvariantCulture, donation.Amount)} XMR ");
        sb.Append(donation switch
        {
            { IsSpent: true } => "📨",
            { IsUnlocked: true } => "💰",
            { IsUnlocked: false } => "⏳"
        });
        sb.AppendLine();
        sb.Append($"Total Bounty: {FormatAtomicAmount(CultureInfo.InvariantCulture, total)} XMR");
        return sb.ToString();
    }

    public async Task HandleAsync(SynchronizePostDonationComments command, CancellationToken token = default)
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(token);
        var bounty = await context.Bounties
            .Include(b => b.Donations)
            .Where(b => b.PostNumber == command.PostNumber)
            .SingleOrDefaultAsync(token);
        if (bounty is null)
        {
            this.logger.LogWarning(
                "Attempted to synchronize donation comments for post #{PostNumber} which has not been registered in the database",
                command.PostNumber);
            return;
        }

        var addresses = await context.Entry(bounty)
            .Collection(b => b.DonationAddresses!)
            .Query()
            .Select(da => da.Address)
            .ToListAsync(token);

        var transfers = await this.GetOrderedDonationTransfersAsync(addresses, token);
        if (transfers is null)
        {
            this.logger.LogError(
                "Failed to get the ordered transfers for post #{PostNumber} using addresses {@Addresses} - due to this error the post " +
                "cannot be synchronized",
                bounty.PostNumber,
                addresses);
            return;
        }

        var comments =
            (await this.getDonationComments.HandleAsync(new GetDonationComments((int) command.PostNumber), token))
            .OrderBy(c => c.CommentId)
            .Select(c => new Comment(CommentId: c.CommentId, Content: c.Content))
            .ToList();
        var donations = transfers
            .Select((t, i) => t switch
            {
                ConfirmedTransfer c => new Donation(
                    Order: i,
                    Amount: c.Amount,
                    IsSpent: c.IsSpent,
                    IsUnlocked: c.IsUnlocked,
                    Transfer: c),
                MempoolTransfer m => new Donation(
                    Order: i,
                    Amount: m.Amount,
                    IsSpent: false,
                    IsUnlocked: false,
                    Transfer: t),
                _ => throw new NotImplementedException()
            })
            .ToList();

        var edits = new List<Edit>();
        var count = Math.Max(comments.Count, donations.Count);
        for (var i = 0; i < count; i++)
        {
            var donation = i < donations.Count ? donations[i] : null;
            var comment = i < comments.Count ? comments[i] : null;
            edits.Add((donation, comment) switch
            {
                ({ } d, null) => new CreateComment(FormatDonationAsComment(d, donations), d),
                (null, { } c) => new DeleteComment(c.CommentId),
                ({ } d, { } c) when FormatDonationAsComment(d, donations) is { } content && content != c.Content
                    => new UpdateComment(c.CommentId, content, donation),
                ({ } d, { } c) => new NoOp(c.CommentId, d),
                (null, null) => throw new NotImplementedException()
            });
        }

        if (edits.Any(e => e is not NoOp))
        {
            this.logger.LogInformation(
                "Performing the following comment edits for post #{PostNumber} in order to synchronize the existing comments {@Comments} with the detected donations {@Donations}: {@Edits}",
                command.PostNumber,
                comments,
                donations,
                edits);
        }
        else
        {
            this.logger.LogTrace(
                "The post #{PostNumber}'s existing comments are synchornized with the detected donations",
                command.PostNumber);
        }

        /* when 'applying' the edits we want to (where possible) firstly ensure the database matches our intentions,
         * and _then_ cause the edit to happen by calling the fider API. The reason we do this is so that if the database
         * save fails for whatever reason we avoid causing the effect without it being reflected in our internal
         * database. Note that the presence of the effect in our database has no bearing on the list of edits we need
         * to apply on the next go around - the edits are only based off the comments as retrieved by the fider
         * API and the wallet's transfers. Basically we never want the database to be _behind_ the fider reality, and
         * it is okay if it is ahead because fider will _eventually_ become consistent. Now you obviously can't do
         * this in the case of the create comment edit without making `CommentId` on the `Donation` entity nullable,
         * and then setting it on a second pass where there would be a 'NoOp' edit associated with it. In principal
         * this may be a good idea, however you would need another column with a flag such that you can create a
         * constraint ensuring that if only if the 'created' flag is set that the FK can be null - I'd rather not
         * do that at this stage.
         *
         * Also, the easiest way to implement this is to clear all the donations for a bounty and then create them
         * as per the edits - this is a _lot_ simpler than trying to reconcile differently for each type of edit
         * especially when you consider that our database may not have a record for existing comments/donations when
         * restoring from the fider API!
         */

        await using var transaction = await context.Database.BeginTransactionAsync(token);

        context.Donations.RemoveRange(bounty.Donations!);
        await context.SaveChangesAsync(token);
        bounty.Donations!.Clear();

        static Db.Donation MapDonationToDbEntity(Donation donation, int commentId)
        {
            return new Db.Donation(
                txHash: donation.Transfer.TxHash,
                address: donation.Transfer.Destination.Address,
                amount: donation.Transfer.Amount,
                commentId: commentId);
        }
        
        foreach (var edit in edits)
        {   
            switch (edit)
            {
                case DeleteComment delete:
                    await this.fider.DeleteCommentAsync(
                        (int) command.PostNumber,
                        delete.CommentId,
                        CancellationToken.None);
                    break;
                /* an _update_ edit typically means that the existing donation comments are:
                 * a) out of order,
                 * b) do not match the transfer record
                 * c) the formatting logic has changed
                 */
                case UpdateComment update:
                {
                    bounty.Donations.Add(MapDonationToDbEntity(update.Donation, update.CommentId));
                    await context.SaveChangesAsync(CancellationToken.None);

                    /* the `Donation` in the database now correctly reflects it's associated transfer - however the
                     * associated comment in Fider has the incorrect content - so we try make it match.
                     */
                    await this.fider.UpdateCommentAsync(
                        (int) command.PostNumber,
                        update.CommentId,
                        update.Content,
                        CancellationToken.None);
                    break;
                }
                /* this is the most straightforward case - we need to create a comment when we receive a donation
                 * which hasn't already had a comment posted for it!
                 */
                case CreateComment create:
                {
                    /* this step is the exception to the rule where we update the database first - we can't do this
                     * here because we require the comment id to create the `Donation` database entry... as outlined
                     * above there are ways around this but for now this is simpler.
                     */
                    var commentId = await this.fider.PostCommentAsync(
                        (int) command.PostNumber,
                        create.Content,
                        new List<ImageUpload>(),
                        CancellationToken.None);
                    bounty.Donations.Add(MapDonationToDbEntity(create.Donation, commentId));
                    await context.SaveChangesAsync(CancellationToken.None);
                    break;
                }
                case NoOp noop:
                {
                    /* our fider comment list (actual) and expected comment list (transfers) may match exactly -
                     * but the corresponding record not exist in our database. This will be the case when restoring
                     * from the fider API after deleting a database.
                     */
                    bounty.Donations.Add(MapDonationToDbEntity(noop.Donation, noop.CommentId));
                    await context.SaveChangesAsync(CancellationToken.None);
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }
            
        await transaction.CommitAsync(CancellationToken.None);
    }

    private async Task<IReadOnlyCollection<Transfer>?> GetOrderedDonationTransfersAsync(IEnumerable<string> addresses, CancellationToken token = default)
    {
        var transfers = new List<Transfer>();
        foreach (var address in addresses)
        {
            var index = await this.getAddressIndex.HandleAsync(new GetAddressIndex(address), token);
            if (index is null)
            {
                this.logger.LogError(
                    "Failed to determine the index for {Address}",
                    address);
                return null;
            }

            var result = await this.getIncomingTransfers.HandleAsync(new GetIncomingTransfers(new Destination(address, index.Major, index.Minor)), token);
            if (result is null)
            {
                this.logger.LogError(
                    "Failed to fetch the incoming transfers for {Address}",
                    address);
                return null;
            }

            transfers.AddRange(result.Transfers);
        }

        return transfers;
    }

    private record Donation(int Order, ulong Amount, bool IsSpent, bool IsUnlocked, Transfer Transfer);

    private record Comment(int CommentId, string Content);

    private abstract record Edit;

    private record DeleteComment(int CommentId) : Edit;

    private record UpdateComment(int CommentId, string Content, Donation Donation) : Edit;

    private record CreateComment(string Content, Donation Donation) : Edit;

    private record NoOp(int CommentId, Donation Donation) : Edit;
}
