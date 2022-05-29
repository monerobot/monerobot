namespace MoneroBot.Daemon.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneroBot.Daemon.Features;

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
            await this.PerformRegistrations(token);
        }
        catch (Exception exception)
        {
            this.logger.LogCritical("An unhandled exception occured whilst performing registrations: {exception}", exception);
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

        var posts = await this.getUnregisteredPosts.HandleAsync(new GetUnregisterdPosts(), token);
        if (posts.Count is 0)
        {
            this.logger.LogTrace("There are no more posts to register at this time");
            return;
        }

        foreach (var (postNumber, isExistingBounty) in posts)
        {
            int? id;

            if (isExistingBounty)
            {
                this.logger.LogInformation("Registering post #{post_number} as a  bounty", postNumber);
                id = await this.registerExistingBounty.HandleAsync(new RegisterExistingBounty(postNumber), token);
            }
            else
            {
                this.logger.LogInformation("Importing post #{post_number} as a new bounty", postNumber);
                id = await this.registerNewBounty.HandleAsync(new RegisterNewBounty(postNumber, this.options.WalletAccountIndex));
            }

            if (id is not null)
            {
                this.logger.LogInformation(
                    "Successfully registered a bounty (id = {bounty_id}) for post #{post_number}",
                    id,
                    postNumber);
            }
            else
            {
                this.logger.LogWarning(
                    "Failed to register a bounty for post #{post_number} for an unkown reason",
                    postNumber);
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
