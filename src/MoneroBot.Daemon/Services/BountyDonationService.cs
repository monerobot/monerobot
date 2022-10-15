namespace MoneroBot.Daemon.Services;

using Database;
using Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Db = Database.Entities;

internal class BountyDonationService : IHostedService, IDisposable
{
    private readonly DaemonOptions options;
    private readonly ILogger<BountyDonationService> logger;
    private readonly IDbContextFactory<MoneroBotContext> contextFactory;
    private readonly ISynchronizePostDonationCommentsHandler synchronizePostDonationComments;
    private CancellationTokenSource? cts;
    private Timer? timer;

    public BountyDonationService(
        IOptions<DaemonOptions> options,
        ILogger<BountyDonationService> logger,
        IDbContextFactory<MoneroBotContext> contextFactory,
        ISynchronizePostDonationCommentsHandler synchronizePostDonationComments)
    {
        this.options = options.Value;
        this.logger = logger;
        this.contextFactory = contextFactory;
        this.synchronizePostDonationComments = synchronizePostDonationComments;
    }

    public Task StartAsync(CancellationToken token)
    {
        this.logger.LogInformation("The bounty contribution service which scans for donations has started");
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
            await this.ProcessDonationsAsync(token);
        }
        catch (Exception exception)
        {
            this.logger.LogCritical(exception, "An unhandled exception occured whilst scanning for contributions");
        }
        finally
        {
            await Task.Delay(this.options.PollingInterval);
            this.timer?.Change(0, Timeout.Infinite);
        }
    }

    private async Task ProcessDonationsAsync(CancellationToken token = default)
    {
        await using var context = await this.contextFactory.CreateDbContextAsync(token);

        var bounties = await context.Bounties
            .Select(b => new
            {
                b.PostNumber,
            })
            .ToListAsync(token);
        foreach (var bounty in bounties)
        {
            try
            {
                await this.synchronizePostDonationComments.HandleAsync(
                    new SynchronizePostDonationComments(bounty.PostNumber),
                    token);
            }
            catch (Exception error)
            {
                this.logger.LogError(error, "An unexpected error occured whilst trying to synchronize the post #{PostNumber} with the donations.", bounty.PostNumber);
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
