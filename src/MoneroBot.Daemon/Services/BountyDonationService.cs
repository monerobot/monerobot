namespace MoneroBot.Daemon.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneroBot.Daemon.Features;
using MoneroBot.Database;
using Db = Database.Entities;
using MoneroBot.Fider;
using MoneroBot.WalletRpc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

internal class BountyDonationService : IHostedService, IDisposable
{
    private const decimal ATOMIC_TO_MONERO_SCALER = 1e-12m;
    private readonly DaemonOptions options;
    private readonly ILogger<BountyDonationService> logger;
    private readonly MoneroBotContext context;
    private readonly IGetAddressIndexHander getAddressIndex;
    private readonly IGetIncomingTransfersHandler getIncomingTransfers;
    private readonly IFiderApiClient fider;
    private readonly IWalletRpcClient wallet;
    private CancellationTokenSource? cts;
    private Timer? timer;

    public BountyDonationService(
        IOptions<DaemonOptions> options,
        ILogger<BountyDonationService> logger,
        MoneroBotContext context,
        IGetAddressIndexHander getAddressIndex,
        IGetIncomingTransfersHandler getIncomingTransfers,
        IFiderApiClient fider,
        IWalletRpcClient wallet)
    {
        this.options = options.Value;
        this.logger = logger;
        this.context = context;
        this.getAddressIndex = getAddressIndex;
        this.getIncomingTransfers = getIncomingTransfers;
        this.fider = fider;
        this.wallet = wallet;
    }

    private static string FormatAtomicAmount(CultureInfo culture, ulong amount)
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

    public Task StartAsync(CancellationToken token)
    {
        this.logger.LogInformation("The bounty contribution service which scans for donations has started");
        this.cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        this.timer = new(this.Tick, null, 0, Timeout.Infinite);
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
            await this.SynchronizeImportedEnoteMetadata(token);
            await this.ImportNewDonationsIntoDb(token);
            await this.SynchronizeDonationComments(token);
        }
        catch (Exception exception)
        {
            this.logger.LogCritical("An unhandled exception occured whilst scanning for contributions: {exception}", exception);
        }
        finally
        {
            await Task.Delay(this.options.PollingInterval);
            this.timer?.Change(0, Timeout.Infinite);
        }
    }

    private async Task SynchronizeImportedEnoteMetadata(CancellationToken token = default)
    {
        var addresses = await this.context.DonationAddresses
            .Select(da => da.Address)
            .ToListAsync(token);

        foreach (var address in addresses)
        {
            var index = await this.getAddressIndex.HandleAsync(new GetAddressIndex(address), token);
            if (index is null)
            {
                this.logger.LogCritical(
                    "Failed to determine the index for {address} and therefore cannot synchronize the metadata of any associated enotes",
                    address);
                continue;
            }

            var transfers = await this.getIncomingTransfers.HandleAsync(new GetIncomingTransfers(new Destination(address, index.Major, index.Minor)), token);
            if (transfers is null)
            {
                this.logger.LogCritical(
                    "Failed to fetch the incoming transfers for {address} and therefore cannot synchronize the metadata of any associated enotes",
                    address);
                continue;
            }

            var enotes = await this.context.Enotes
                .Where(e => e.Address == address)
                .ToListAsync(token);

            foreach (var enote in enotes)
            {
                var match = transfers.Transfers
                    .SelectMany(t => t.Enotes.Select(e => new
                    {
                        e.PubKey,
                        t.BlockHeight,
                        e.IsUnlocked,
                        e.IsSpent,
                    }))
                    .FirstOrDefault(e => e.PubKey == enote.PubKey);
                if (match is null)
                {
                    this.logger.LogCritical(
                        "The donation address {address} has an enote {@enote} which can no longer be found in any transfers - have you changed the bounty wallet?",
                        address,
                        enote);
                    continue;
                }

                enote.BlockHeight = match.BlockHeight;
                enote.IsSpent = match.IsSpent;
                enote.IsUnlocked = match.IsUnlocked;
                await this.context.SaveChangesAsync(token);
            }
        }
    }

    private async Task ImportNewDonationsIntoDb(CancellationToken token = default)
    {
        var donationAddresses = await this.context.DonationAddresses
            .Include(da => da.Bounty)
            .ToListAsync(token);

        foreach (var donationAddress in donationAddresses)
        {
            var bounty = donationAddress.Bounty!;

            var index = await this.getAddressIndex.HandleAsync(new GetAddressIndex(donationAddress.Address), token);
            if (index is null)
            {
                this.logger.LogCritical(
                    "Failed to determine the index for {address} and therefore cannot search for new donations",
                    donationAddress.Address);
                continue;
            }

            var transfers = await this.getIncomingTransfers.HandleAsync(new GetIncomingTransfers(new Destination(donationAddress.Address, index.Major, index.Minor)), token);
            if (transfers is null)
            {
                this.logger.LogCritical(
                    "Failed to fetch the incoming transfers for {address} and therefore cannot search for new donations",
                    donationAddress.Address);
                continue;
            }

            var enotes = await this.context.Entry(donationAddress)
                .Collection(a => a.Donations!)
                .Query()
                .SelectMany(d => d.DonationEnotes!.Select(de => de.Enote!))
                .ToListAsync(token);
            var scanHeight = enotes
                .Select(e => e.BlockHeight)
                .DefaultIfEmpty(0ul)
                .Max();
            var donationTransfers = transfers.Transfers
                .OrderBy(t => t.BlockHeight)
                .Where(t => t.BlockHeight >= scanHeight)
                .SelectMany(t => t.Enotes.Select(e => new
                {
                    t.TxHash,
                    t.BlockHeight,
                    Enote = e,
                }))
                .Where(txo => enotes.Any(pe => pe.PubKey == txo.Enote.PubKey) is false)
                .GroupBy(txo => new { txo.TxHash, txo.BlockHeight })
                .Select(g => new
                {
                    g.Key.TxHash,
                    g.Key.BlockHeight,
                    Enotes = g.Select(txo => txo.Enote).ToList(),
                })
                .ToList();
            foreach (var donationTransfer in donationTransfers)
            {
                this.logger.LogInformation(
                    "Detected new donation post #{post_number}: {@donation}",
                    donationAddress.Bounty!.PostNumber,
                    donationTransfer);
                var donation = new Db.Donation(donationAddress)
                {
                    DonationEnotes = new List<Db.DonationEnote>(),
                };

                foreach (var enote in donationTransfer.Enotes)
                {
                    donation.DonationEnotes.Add(new Db.DonationEnote(donation, new Db.Enote(donationAddress.Address, enote.PubKey, donationTransfer.TxHash)
                    {
                        BlockHeight = donationTransfer.BlockHeight,
                        Amount = enote.Amount,
                        IsSpent = enote.IsSpent,
                        IsUnlocked = enote.IsUnlocked,
                    }));
                }

                donationAddress.Donations!.Add(donation);
            }

            await this.context.SaveChangesAsync(token);
        }
    }

    private async Task SynchronizeDonationComments(CancellationToken token = default)
    {
        var posts = (await this.context.Donations
            .Select(d => new
            {
                d.DonationAddress!.Bounty!.PostNumber,
                Donation = d,
                Comment = (Db.Comment?)d.Comment,
                Enotes = d.DonationEnotes!.Select(de => de.Enote!).ToList(),
            })
            .ToListAsync(token))
            .GroupBy(d => d.PostNumber)
            .Select(g => new
            {
                PostNumber = g.Key,
                Donations = g
                    .Select(d => new
                    {
                        Donation = d.Donation,
                        Comment = (Db.Comment?)d.Comment,
                        d.Enotes,
                    })
                    .OrderBy(d => d.Enotes.Min(e => e.BlockHeight))
                    .ThenBy(d => d.Donation.Id)
                    .ToList(),
            })
            .ToList();
        foreach (var post in posts)
        {
            var total = 0ul;

            foreach (var donation in post.Donations)
            {
                if (donation.Enotes.Count is 0)
                {
                    this.logger.LogCritical(
                        "Somehow the donation {@donation} for post #{post_number} ended up with no enotes - this shouldn't ever happen " +
                        "because a donation is only created when a transfer has at least one enote to the donation address... ending up in " +
                        "this state is either the result of a bug or manually deleting records from the database",
                        post.PostNumber);
                    continue;
                }

#pragma warning disable SA1101 // Prefix local calls with this
                var summary = donation.Enotes.Aggregate(
                    new { Amount = 0ul, IsSpent = true, IsUnlocked = true },
                    (a, e) => a with
                    {
                        Amount = a.Amount + e.Amount,
                        IsSpent = a.IsSpent && e.IsSpent,
                        IsUnlocked = a.IsSpent && e.IsUnlocked,
                    });
#pragma warning restore SA1101 // Prefix local calls with this
                total += summary.Amount;

                var sb = new StringBuilder();
                sb.Append($"Bounty increased by {FormatAtomicAmount(CultureInfo.InvariantCulture, summary.Amount)} XMR ");
                sb.Append(summary switch
                {
                    { IsSpent: true } => "📨",
                    { IsUnlocked: true } => "💰",
                    { IsUnlocked: false } => "⏳"
                });
                sb.AppendLine();
                sb.Append($"Total Bounty: {FormatAtomicAmount(CultureInfo.InvariantCulture, total)} XMR");
                var content = sb.ToString();

                if (donation.Comment is not null)
                {
                    /* point of no return */
                    try
                    {
                        var comment = donation.Comment;
                        if (comment.Content != content)
                        {
                            using var transaction = await this.context.Database.BeginTransactionAsync(token);
                            await this.fider.UpdateCommentAsync((int)post.PostNumber, comment.CommentId, content, CancellationToken.None);
                            comment.Content = content;
                            await transaction.CommitAsync();
                            this.logger.LogInformation(
                                "Successfully updated comment {@comment} for donation {@donation} beloning to post #{post_number}",
                                comment,
                                summary,
                                post.PostNumber);
                        }
                    }
                    catch (HttpRequestException exception)
                    {
                        this.logger.LogCritical(
                            "Failed to update comment for donation {@donation} beloning to post #{post_number}: {@fider_error}",
                            summary,
                            post.PostNumber,
                            exception);
                    }
                }
                else
                {
                    try
                    {
                        using var transaction = await this.context.Database.BeginTransactionAsync(token);
                        var id = await this.fider.PostCommentAsync((int)post.PostNumber, content, new(), CancellationToken.None);
                        var comment = new Db.Comment(id, content);
                        donation.Donation.Comment = comment;
                        await transaction.CommitAsync();
                        this.logger.LogInformation(
                            "Successfully created comment {@comment} for donation {@donation} beloning to post #{post_number}",
                            comment,
                            summary,
                            post.PostNumber);
                    }
                    catch (HttpRequestException exception)
                    {
                        this.logger.LogCritical(
                            "Failed to create comment for donation {@donation} beloning to post #{post_number}: {@fider_error}",
                            summary,
                            post.PostNumber,
                            exception);
                    }
                }
            }
        }
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
