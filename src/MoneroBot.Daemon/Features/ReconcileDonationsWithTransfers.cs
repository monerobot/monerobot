namespace MoneroBot.Daemon.Features;

using Microsoft.Extensions.Logging;
using System.Collections.Generic;

public record Donation(int CommentId, ulong Amount, ulong TotalAmount);
public record ReconciledDonation(int CommentId, Transfer Transfer);

public record ReconcileDonationsWithTransfers(List<Donation> Donations, List<Transfer> Transfers, ulong? LegacyBlockHeight);

public record ReconciledDonations(List<ReconciledDonation> Donations);

public interface IReconcileDonationsWithTransfers
{
    public Task<ReconciledDonations> HandleAsync(ReconcileDonationsWithTransfers request, CancellationToken token = default);
}

public class ReconcileDonationsWithTransfersHandler : IReconcileDonationsWithTransfers
{
    private readonly ILogger<ReconcileDonationsWithTransfersHandler> logger;

    public ReconcileDonationsWithTransfersHandler(ILogger<ReconcileDonationsWithTransfersHandler> logger)
    {
        this.logger = logger;
    }

    public Task<ReconciledDonations> HandleAsync(ReconcileDonationsWithTransfers request, CancellationToken token = default)
    {
        var donations = request.Donations.OrderBy(d => d.TotalAmount).ToList();
        var transfers = request.Transfers.OrderBy(t => t.BlockHeight).GetEnumerator();

        var reconciled = new List<ReconciledDonation>();
        foreach (var donation in donations)
        {
            var matches = false;

            while (transfers.MoveNext())
            {
                var transfer = transfers.Current;
                var amount = transfer.Enotes.Aggregate(0ul, (a, e) => a + e.Amount);
                if (amount == donation.Amount)
                {
                    this.logger.LogInformation(
                        "Matched the donation {@donation} with transfer {@transfer} due to an exact amount match",
                        donation,
                        transfer);
                    matches = true;
                }

                /* here we need to support the legacy code which incorrectly assumes the `incoming_transfers` RPC call
                 * would return a _single_ entry for a transaction which contained many transfers/enotes. Instead the RPC
                 * call returns an entry for each enote - however the old code would find the last enote:
                 * 
                 * ```
                 * for _, transfer := range incomingTransfersResp.Transfers {
                 * 	if transfer.TxHash == txToBeProcessed.TxHash {
                 * 		txToBeProcessed.Amount = transfer.Amount
                 * 		txToBeProcessed.GlobalIndex = transfer.GlobalIndex
                 * 		txToBeProcessed.KeyImage = transfer.KeyImage
                 * 		txToBeProcessed.Spent = transfer.Spent
                 * 		txToBeProcessed.SubaddrIndex = transfer.SubaddrIndex
                 * 		txToBeProcessed.TxSize = transfer.TxSize
                 * 	}
                 * }
                 * ```
                 */
                else if (request.LegacyBlockHeight is { } legacyBlockHeight
                    && transfer.BlockHeight <= legacyBlockHeight
                    && transfer.Enotes.Last().Amount is { } lastEnoteAmount
                    && lastEnoteAmount == donation.Amount)
                {
                    this.logger.LogInformation(
                        "Matched a legacy system donation {@donation} with transfer {@transfer} due to the transfer occuring at" +
                        " block height {block_height} which occures at or before the configured legacy block height of {legacy_block_height}" +
                        " and the last enote amount ({last_enote_amount}) matching the donation comment amount {comment_amount}. Consequently the donation" +
                        " amount has increased from {legacy_amount} to {new_amount} to include the skipped enotes.",
                        donation,
                        transfer,
                        transfer.BlockHeight,
                        legacyBlockHeight,
                        lastEnoteAmount,
                        donation.Amount,
                        donation.Amount,
                        amount);
                    matches = true;
                }
                else
                {
                    this.logger.LogWarning(
                        "The transfer {@transfer} was not matched to any of the following donation comments: {@donations}",
                        transfer,
                        donations);
                    matches = false;
                }

                if (matches)
                {
                    reconciled.Add(new ReconciledDonation(donation.CommentId, transfer));
                    break;
                }
            }

            if (matches is false)
            {
                this.logger.LogCritical(
                    "Failed to reconcile the donation {@donation} with any of the following transfers: {@transfers}",
                    donation,
                    transfers);
            }
        }

        return Task.FromResult(new ReconciledDonations(reconciled)); 
    }
}
