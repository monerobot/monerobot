namespace MoneroBot.Daemon.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneroBot.Database;
using MoneroBot.Database.Entities;
using MoneroBot.Fider;
using MoneroBot.Fider.Models;
using MoneroBot.WalletRpc;
using MoneroBot.WalletRpc.Models;
using MoneroBot.WalletRpc.Models.Generated;
using System.Globalization;
using System.Numerics;
using System.Text;

internal class BountyContributionService : IHostedService, IDisposable
{
    private readonly DaemonOptions options;
    private readonly ILogger<BountyContributionService> logger;
    private readonly IDbContextFactory<MoneroBotContext> contextFactory;
    private readonly IFiderApiClient fiderApi;
    private readonly IWalletRpcClient walletRpc;
    private MoneroBotContext? context;
    private CancellationTokenSource? cts;
    private Timer? timer;

    public BountyContributionService(
        IOptions<DaemonOptions> options,
        ILogger<BountyContributionService> logger,
        IDbContextFactory<MoneroBotContext> contextFactory,
        IFiderApiClient fiderApi,
        IWalletRpcClient walletRpc)
    {
        this.options = options.Value;
        this.logger = logger;
        this.contextFactory = contextFactory;
        this.fiderApi = fiderApi;
        this.walletRpc = walletRpc;
    }

    public async Task StartAsync(CancellationToken token)
    {
        this.logger.LogInformation("Bounty contribution service has started");

        this.context = await this.contextFactory.CreateDbContextAsync(token);
        this.cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        this.timer = new (this.Tick, null, 0, Timeout.Infinite);
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
            await this.ScanForContributions(token);
        }
        catch (Exception error)
        {
            this.logger.LogCritical("Unhandled exception occured whilst scanning for contributions: {error}", error);
        }
        finally
        {
            await Task.Delay(this.options.PollingInterval);
            this.timer?.Change(0, Timeout.Infinite);
        }
    }

    private async Task ScanForContributions(CancellationToken token)
    {
        var incomingTransfers = await this.GetIncomingTransfersForBountiesAsync(token);
        if (incomingTransfers.Any() is false)
        {
            this.logger.LogInformation("No incoming transfers were detected for any bounties - is something wrong?");
            return;
        }

        foreach (var (bounty, transfers) in incomingTransfers)
        {
            foreach (var transfer in transfers.OrderBy(t => t.BlockHeight).ThenBy(t => t.TxHash))
            {
                try
                {
                    await this.SynchronizeBountyContributionWithTransferAsync(bounty, transfer, token);
                }
                catch (Exception error)
                {
                    this.logger.LogCritical(
                        "An unhandled exception occured whilst trying to synchronize post #{number} with the transfer {transaction}: {error}",
                        bounty.PostNumber,
                        transfer.TxHash,
                        error);
                }
            }
        }
    }

    private async Task<bool> SynchronizeBountyContributionWithTransferAsync(Bounty bounty, TransferDetails transfer, CancellationToken token)
    {
        var maybeExistingContribution = await this.TryGetExistingBountyContributionForTransferAsync(transfer, token);
        if (maybeExistingContribution.TryUnwrapValue(out var contribution))
        {
            if (contribution.BountyId != bounty.Id)
            {
                this.logger.LogCritical(
                    "Somehow the transfer {transfer} was associated with the wrong bounty - it was associated with post #{actual} but it was expected to be associated with post #{expected}",
                    transfer.TxHash,
                    contribution.BountyId,
                    bounty.Id);
                return false;
            }

            return await this.UpdateBountyContributionWithTransferDetailsAsync(bounty, contribution, transfer, token);
        }

        return await this.CreateBountyContributionWithTransferDetailsAsync(bounty, transfer, token);
    }

    private async Task<bool> CreateBountyContributionWithTransferDetailsAsync(Bounty bounty, TransferDetails transfer, CancellationToken token)
    {
        using var transaction = await this.context!.Database.BeginTransactionAsync(token);

        var xmr = new XmrTransaction(
            transactionId: transfer.TxHash,
            blockHeight: transfer.BlockHeight,
            accountIndex: bounty.AccountIndex,
            subAddressIndex: bounty.SubAddressIndex,
            subAddress: bounty.SubAddress,
            amount: transfer.Amount,
            isSpent: transfer.Spent,
            isUnlocked: transfer.Unlocked);
        var contribution = new BountyContribution(bounty, xmr, commentId: 0);

        this.context.XmrTransactions.Add(xmr);
        this.context.BountyContributions.Add(contribution);

        var content = await this.FormatBountyContributionToCommentContentAsync(bounty, contribution, token);
        /* point of no return */
        var createCommentResponse = await this.fiderApi.PostCommentAsync(bounty.PostNumber, content, new ());
        if (createCommentResponse.Error is { } createCommentErr)
        {
            this.logger.LogCritical(
                "Failed to create contribution recieved comment for transfer {transaction} beloning to post #{number}",
                transfer.TxHash,
                bounty.PostNumber);
            createCommentErr.Log(this.logger, LogLevel.Critical);
            return false;
        }

        contribution.CommentId = createCommentResponse.Result;
        await this.context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    private async Task<bool> UpdateBountyContributionWithTransferDetailsAsync(Bounty bounty, BountyContribution contribution, TransferDetails transfer, CancellationToken token)
    {
        using var transaction = await this.context!.Database.BeginTransactionAsync(token);

        await this.context
            .Entry(contribution)
            .Reference(c => c.Transaction)
            .LoadAsync(token);

        if (contribution.Transaction!.TransactionId != transfer.TxHash)
        {
            this.logger.LogCritical(
                "Attempted to update the transfer {transaction} which is a contribution to post #{number} with the details of a different transaction {other_transaction}",
                contribution.Transaction.TransactionId,
                bounty.PostNumber,
                transfer.TxHash);
            return false;
        }

        var changed =
            contribution.Transaction.BlockHeight != transfer.BlockHeight
            || contribution.Transaction.IsSpent != transfer.Spent
            || contribution.Transaction.IsUnlocked != transfer.Unlocked;

        if (!changed)
        {
            this.logger.LogTrace(
                "No changes detected for transaction {transaction} beloning to post #{number}",
                transfer.TxHash,
                bounty.PostNumber);
            return true;
        }

        contribution.Transaction!.BlockHeight = transfer.BlockHeight;
        contribution.Transaction!.IsSpent = transfer.Spent;
        contribution.Transaction!.IsUnlocked = transfer.Unlocked;

        var content = await this.FormatBountyContributionToCommentContentAsync(bounty, contribution, token);
        /* point of no return */
        var updateCommentResposne = await this.fiderApi.UpdateCommentAsync(postNumber: bounty.PostNumber, commentId: contribution.CommentId, content);
        if (updateCommentResposne.Error is { } updateCommentErr)
        {
            this.logger.LogCritical(
                "Failed to update contribution comment for transfer {transaction} beloning to post #{number}",
                transfer.TxHash,
                bounty.PostNumber);
            updateCommentErr.Log(this.logger, LogLevel.Critical);
            return false;
        }

        await this.context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    private async Task<string> FormatBountyContributionToCommentContentAsync(Bounty bounty, BountyContribution contribution, CancellationToken token)
    {
        await this.context!
            .Entry(contribution)
            .Reference(c => c.Transaction!)
            .LoadAsync(token);
        var previousTotal = (await this.context
            .Entry(bounty)
            .Collection(b => b.Contributions!)
            .Query()
            /* if the contribution came in but is not tied to a comment (.e `CommentId` is 0)
             * then it will be treated as the latest contribution and thus the previous total
             * is the sum of all the previous contributions. If we are calculating it for a
             * contribution that isn't the latest, for that contribution the previous total
             * is the sum of all contributions that came before it (we order contributions
             * by the comment id)
             */
            .Where(c => contribution.CommentId == 0 || c.CommentId < contribution.CommentId)
            .Select(c => c.Transaction!.Amount)
            .ToListAsync(token))
            .Aggregate(default(ulong), (total, amnt) => total + amnt);
        var total = previousTotal + contribution.Transaction!.Amount;

        const decimal ATOMIC_TO_MONERO_SCALER = 1e-12m;
        static string FormatAtomicAmount(CultureInfo culture, ulong amount)
        {
            var seperator = culture.NumberFormat.NumberDecimalSeparator;
            var moneros = amount * ATOMIC_TO_MONERO_SCALER;
            if (moneros.ToString(culture).Contains(seperator))
            {
                // A piconero     0.000000000001 is the smallet unit
                return $"{moneros:0.############}";
            }

            return $"{moneros:N0}";
        }

        var sb = new StringBuilder();
        sb.Append($"Bounty increased by {FormatAtomicAmount(CultureInfo.InvariantCulture, contribution.Transaction!.Amount)} XMR ");
        sb.Append(contribution.Transaction switch
        {
            { IsSpent: true } => "📨",
            { IsUnlocked: true } => "💰",
            { IsUnlocked: false } => "⏳"
        });
        sb.AppendLine();
        sb.Append($"Total Bounty: {FormatAtomicAmount(CultureInfo.InvariantCulture, total)} XMR");
        return sb.ToString();
    }

    private async Task<Option<BountyContribution>> TryGetExistingBountyContributionForTransferAsync(TransferDetails transfer, CancellationToken token)
    {
        var contribution = await this.context!.BountyContributions
            .SingleOrDefaultAsync(c => c.TransactionId == transfer.TxHash, token);
        if (contribution is null)
        {
            return Option.None<BountyContribution>();
        }

        return Option.Some(contribution);
    }

    private async Task<List<(Bounty Bounty, List<TransferDetails> Transfers)>> GetIncomingTransfersForBountiesAsync(CancellationToken token)
    {
        /* remember that we allow bounties to be associated with whatever account index the daemon was
         * configured to use at the time, this means that (because the RPC interface only lets you query
         * one account at a time for 'incoming_transfers' we need to make a request for each account
         */
        var bountiesByAccountNumber = (await this.context!.Bounties
            .ToListAsync(token))
            .GroupBy(b => new { b.AccountIndex })
            .ToDictionary(
                g => g.Key.AccountIndex,
                g => g.ToList());

        var results = new List<(Bounty Bounty, List<TransferDetails> Transfers)>();

        foreach (var (accountIndex, bounties) in bountiesByAccountNumber)
        {
            this.logger.LogInformation(
                "Fetching incoming transfers for wallet (account #{account}) which has {count} associated bounties",
                accountIndex,
                bounties.Count);

            var bountySubaddressIndexes = bounties
                .Select(b => b.SubAddressIndex)
                .ToHashSet();
            var getIncomingTransfersRequest = new MoneroRpcRequest(
                "incoming_transfers",
                new IncomingTransfersParameters(
                    transferType: "all",
                    accountIndex: accountIndex,
                    subaddrIndices: bountySubaddressIndexes));
            var getIncomingTransfersResponse = await this.walletRpc.JsonRpcAsync<IncomingTransfersResult>(getIncomingTransfersRequest, token);
            if (getIncomingTransfersResponse.Error is { } getIncomingTransfersErr)
            {
                this.logger.LogCritical(
                    "Failed to retrieve incoming transfers for wallet (account #{account}) which has {count} associated bounties: ({code}) {message}",
                    accountIndex,
                    bounties.Count,
                    getIncomingTransfersErr.Code,
                    getIncomingTransfersErr.Message);
                continue;
            }

            if (getIncomingTransfersResponse.Result is null)
            {
                this.logger.LogCritical(
                    "Failed to retrieve incoming transfers for wallet (account #{account}) which has {count} associated bounties - the RPC server returned an error but it had no results",
                    accountIndex,
                    bounties.Count);
                continue;
            }

            this.logger.LogInformation(
                "Successfully retrieved incoming transfers for wallet (account #{account}) which has {count} associated bounties",
                accountIndex,
                bounties.Count);

            var transfersByAddress = (getIncomingTransfersResponse.Result.Transfers ?? new ())
                .ToLookup(t => new { AccountIndex = t.SubaddrIndex.Major, SubAddressIndex = t.SubaddrIndex.Minor });
            foreach (var bounty in bounties)
            {
                var address = new { bounty.AccountIndex, bounty.SubAddressIndex };
                var transfers = transfersByAddress[address].ToList();
                this.logger.LogTrace(
                    "Detected #{transfers} for post #{number} ({address})",
                    transfers.Count,
                    bounty.PostNumber,
                    bounty.SubAddress);
                results.Add((bounty, transfers));
            }
        }

        return results;
    }

    /// <inheritdoc />
#pragma warning disable SA1202 // Elements should be ordered by access
    public void Dispose()
#pragma warning restore SA1202 // Elements should be ordered by access
    {
        this.context?.Dispose();
        this.timer?.Dispose();
    }
}
