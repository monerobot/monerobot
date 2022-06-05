namespace MoneroBot.Daemon.Services;

using Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class BountyRegistrationService : IHostedService, IDisposable
{
    private readonly DaemonOptions options;
    private readonly ILogger<BountyRegistrationService> logger;
    private readonly IGetUnregisteredPostsHandler getUnregisteredPosts;
    private readonly IRegisterExistingBountyHandler registerExistingBounty;
    private readonly IRegisterNewBountyHandler registerNewBounty;
    private CancellationTokenSource? cts;
    private Timer? timer;

    public BountyRegistrationService(
        IOptions<DaemonOptions> options,
        ILogger<BountyRegistrationService> logger,
        IGetUnregisteredPostsHandler getUnregisteredPosts,
        IRegisterExistingBountyHandler registerExistingBounty,
        IRegisterNewBountyHandler registerNewBounty)
    {
        this.options = options.Value;
        this.logger = logger;
        this.getUnregisteredPosts = getUnregisteredPosts;
        this.registerExistingBounty = registerExistingBounty;
        this.registerNewBounty = registerNewBounty;
    }

    public Task StartAsync(CancellationToken token)
    {
        this.logger.LogInformation("The Bounty registration service which creates bounties for posts has started");

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
            await this.PerformRegistrations(token);
        }
        catch (Exception exception)
        {
            this.logger.LogCritical(exception, "An unhandled exception occured whilst performing registrations");
        }
        finally
        {
            await Task.Delay(this.options.PollingInterval);
            this.timer?.Change(0, Timeout.Infinite);
        }
    }

    private async Task PerformRegistrations(CancellationToken token = default)
    {
        this.logger.LogTrace("Scanning for posts to register bounties for");

        var posts = await this.getUnregisteredPosts.HandleAsync(new GetUnregisteredPosts(), token);
        if (posts.Count is 0)
        {
            this.logger.LogTrace("There are no more posts to register at this time");
            return;
        }

        var existing = posts.Where(p => p.IsExistingBounty).ToList();
        var @new = posts.Except(existing).ToList();

        /* we need to import any existing posts first because we want to avoid giving out an address which
         * is in use by a different existing post! And the only way we can check that is by first importing any
         * existing posts so that the information is in our database.
         */
        foreach (var post in existing)
        {
            this.logger.LogInformation("Registering post #{PostNumber} as a  bounty", post.PostNumber);
            var id = await this.registerExistingBounty.HandleAsync(new RegisterExistingBounty(post.PostNumber), token);
            if (id is not null)
            {
                this.logger.LogInformation(
                    "Successfully registered a bounty (id = {BountyId}) for existing post #{PostNumber}",
                    id,
                    post.PostNumber);
            }
            else
            {
                this.logger.LogError(
                    "Failed to register a bounty for post #{PostNumber} for an unknown reason - because " +
                    "this is an existing post the import process will halt and try again later. All existing posts " +
                    "must be imported before new ones can be processed so as to ensure that donation addresses are never " +
                    "reused. And we can only detect re-use if we know what donation addresses are already in use. ",
                    post.PostNumber);
                return;
            }
        }

        foreach (var post in @new)
        {
            this.logger.LogInformation("Importing post #{PostNumber} as a new bounty", post.PostNumber);
            var id = await this.registerNewBounty.HandleAsync(new RegisterNewBounty(post.PostNumber, this.options.WalletAccountIndex), token);
            if (id is not null)
            {
                this.logger.LogInformation(
                    "Successfully registered a bounty (id = {BountyId}) for new post #{PostNumber}",
                    id,
                    post.PostNumber);
            }
            else
            {
                this.logger.LogError(
                    "Failed to register a bounty for post #{PostNumber} for an unknown reason",
                    post.PostNumber);
                return;
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
