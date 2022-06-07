namespace MoneroBot.Daemon.Features;

using Microsoft.Extensions.Logging;
using WalletRpc;
using WalletRpc.Models;
using WalletRpc.Models.Generated;

public record Destination(string Address, uint Major, uint Minor);

public abstract record Transfer(string TxHash, Destination Destination, ulong Amount);

public record ConfirmedTransfer(ulong GlobalIndex, string TxHash, Destination Destination, ulong Amount, bool IsSpent, bool IsUnlocked)
    : Transfer(TxHash, Destination, Amount);

public record MempoolTransfer(ulong Timestamp, string TxHash, Destination Destination, ulong Amount)
    : Transfer(TxHash, Destination, Amount);

public record GetIncomingTransfers(Destination Destination);

public record IncomingTransfers(List<Transfer> Transfers);

public interface IGetIncomingTransfersHandler
{
    public Task<IncomingTransfers?> HandleAsync(GetIncomingTransfers request, CancellationToken token = default);
}

public class GetIncomingTransfersHandler : IGetIncomingTransfersHandler
{
    private readonly ILogger<GetIncomingTransfersHandler> logger;
    private readonly IWalletRpcClient wallet;

    public GetIncomingTransfersHandler(ILogger<GetIncomingTransfersHandler> logger, IWalletRpcClient wallet)
    {
        this.logger = logger;
        this.wallet = wallet;
    }

    public async Task<IncomingTransfers?> HandleAsync(GetIncomingTransfers request, CancellationToken token = default)
    {
        try
        {
            var transfers = new List<Transfer>();
            
            /* as it stands the `incoming_transfers` command does not return transactions in the mempool - there is
             * an issue (https://github.com/monero-project/monero/issues/8375) to enable this but for now we need
             * to do two separate requests. One to get the confirmed transfers (because the other `get_transfers`
             * endpoint does not return the metadata we need!), then another to get the mempool transfers. We'll
             * merge the results into the `transfers` list.
             */
            var confirmed = await this.wallet.JsonRpcAsync<IncomingTransfersResult>(
                new MoneroRpcRequest("incoming_transfers", new IncomingTransfersParameters(
                    transferType: "all",
                    accountIndex: request.Destination.Major,
                    subaddrIndices: new() { request.Destination.Minor })),
                token);
            
            if (confirmed.Error is { } confirmedErr)
            {
                this.logger.LogError(
                    "Failed to retrieve incoming transfers for {Address}: {@WalletRpcError}",
                    request.Destination.Address,
                    confirmedErr);
            }
            else if (confirmed.Result is null)
            {
                this.logger.LogError(
                    "Failed to retrieve incoming transfers for {Address} - the RPC returned a response but it was empty",
                    request.Destination.Address);
            }
            else if (confirmed.Result.Transfers is not null)
            {
                /* remember that the `incoming_transfers` returns a result per-enote for each transfer (this will
                 * allow us to do some pretty cool stuff later on) hence we need to group by the tx hash in order to
                 * work out the 'total' of a transfer.
                 */
                transfers.AddRange(confirmed.Result.Transfers
                    .Select(e => new
                    {
                        e.GlobalIndex,
                        e.TxHash,
                        e.Pubkey,
                        e.Amount,
                        e.BlockHeight,
                        IsSpent = e.Spent,
                        IsUnlocked = e.Unlocked,
                    })
                    .GroupBy(e => new { e.GlobalIndex, e.TxHash, e.BlockHeight })
                    .Select(g => new ConfirmedTransfer(
                        GlobalIndex: g.Key.GlobalIndex,
                        TxHash: g.Key.TxHash,
                        Destination: request.Destination,
                        Amount: g.Aggregate(0ul, (t, e) => t + e.Amount),
                        IsSpent: g.All(e => e.IsSpent),
                        IsUnlocked: g.All(e => e.IsUnlocked)))
                    .ToList());
            }

            var mempool = await this.wallet.JsonRpcAsync<GetTransfersResult>(
                new MoneroRpcRequest("get_transfers", new GetTransfersParameters(
                    @in: false,
                    @out: false,
                    pending: false,
                    failed: false,
                    pool: true,
                    filterByHeight: false,
                    minHeight: default,
                    maxHeight: default,
                    accountIndex: request.Destination.Major,
                    subaddrIndices: new HashSet<uint> { request.Destination.Minor },
                    allAccounts: false)),
                token);

            if (mempool.Error is { } mempoolErr)
            {
                this.logger.LogError(
                    "Failed to retrieve incoming transfers for {Address}: {@WalletRpcError}",
                    request.Destination.Address,
                    mempoolErr);
            }
            else if (mempool.Result is null)
            {
                this.logger.LogError(
                    "Failed to retrieve mempool transfers for {Address} - the RPC returned a response but it was empty",
                    request.Destination.Address);
            }
            else if (mempool.Result.Pool is not null)
            {
                transfers.AddRange(mempool.Result.Pool
                    /* disregard any double spends in the memory pool - you wouldn't want to let these through otherwise
                     * you'd create incorrect comments (which would eventually get cleaned up due to the diffing process
                     * used to synchronize the comments but better safe than sorry)
                     */
                    .Where(t => t.DoubleSpendSeen is false)
                    /* for whatever reason the `Destinations` field of the transfer entry may be null -
                     * if that is null we take the non destination breakdown information as a single transfer.
                     * It is very _important_ that we project out the `Destinations` into individual entries if it exists
                     * so that if someone constructed a transaction which donated to several different bounties (or some
                     * random 3rd party) that we don't count the amount send to the non-bounty addresses(s)!
                     */
                    .SelectMany(t => t.Destinations switch
                    {
                        null => new[]
                        {
                            new {TxHash = t.Txid, t.Timestamp, t.Address, t.Amount},
                        },
                        { } => t.Destinations
                            .Select(d => new
                            {
                                TxHash = t.Txid,
                                t.Timestamp,
                                d.Address,
                                d.Amount,
                            })
                    })
                    /* remember that in the context of the method we asked for the incoming transfers to a _single_
                     * address, so we filter out the individual 'transfers' to those matching the requested destination.
                     */
                    .Where(t => t.Address == request.Destination.Address)
                    /* now - it should never happen that the transaction was already added to the final list of
                     * transfers from the 'completed' collection... but best not to make any assumptions. We prefer
                     * the 'completed' representation of the transfer to the mempool one - hence we exclude any
                     * transactions which we got from the mempool that were already included in our final list.
                     */
                    .ExceptBy(transfers.Select(ct => ct.TxHash), t => t.TxHash)
                    .GroupBy(t => new { t.TxHash })
                    .Select(g => new
                    {
                        g.Key.TxHash,
                        Timestamp = g.Max(t => t.Timestamp),
                        Amount = g.Aggregate(0ul, (s, t) => s + t.Amount),
                    })
                    .OrderBy(r => r.Timestamp)
                    .Select(r => new MempoolTransfer(
                        Timestamp: r.Timestamp,
                        TxHash: r.TxHash,
                        Destination: request.Destination,
                        Amount: r.Amount)));
            }

            return new IncomingTransfers(transfers);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "An unhandled exception occured whilst trying to retrieve incoming transfers for {Address}",
                request.Destination.Address);
        }

        return null;
    }
}
