namespace MoneroBot.Daemon.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneroBot.Daemon.Repositories;
using MoneroBot.Database;
using MoneroBot.Database.Entities;
using MoneroBot.Fider;
using MoneroBot.Fider.Models;
using MoneroBot.WalletRpc;
using MoneroBot.WalletRpc.Models;
using MoneroBot.WalletRpc.Models.Generated;
using QRCoder;
using static QRCoder.QRCodeGenerator;

internal class BountyRegistrationService : IHostedService, IDisposable
{
    private readonly DaemonOptions options;
    private readonly ILogger<BountyRegistrationService> logger;
    private readonly MoneroBotContext context;
    private readonly IFiderApiClient fiderApi;
    private readonly IWalletRpcClient walletRpc;
    private CancellationTokenSource? cts;
    private Timer? timer;

    public BountyRegistrationService(
        IOptions<DaemonOptions> options,
        ILogger<BountyRegistrationService> logger,
        MoneroBotContext context,
        IFiderApiClient fiderApi,
        IWalletRpcClient walletRpc)
    {
        this.options = options.Value;
        this.logger = logger;
        this.context = context;
        this.fiderApi = fiderApi;
        this.walletRpc = walletRpc;
    }

    public Task StartAsync(CancellationToken token)
    {
        this.logger.LogInformation("The Bounty registration service which creates bounties for posts has started");

        this.cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        this.timer = new (this.Tick, null, 0, Timeout.Infinite);

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

        var maybePosts = await this.TryGetPostsToRegisterAsync(token);
        if (maybePosts.TryUnwrapValue(out var posts) is false)
        {
            this.logger.LogTrace("No posts to register at this time");
            return;
        }

        foreach (var post in posts)
        {
            token.ThrowIfCancellationRequested();

            this.logger.LogTrace("Attemping to register post {@post}", post);

            using var transaction = await this.context.Database.BeginTransactionAsync(token);
            try
            {
                var maybeAddress = await this.TryCreateAddressForPostAsync(post, token);
                if (maybeAddress.TryUnwrapValue(out var address))
                {
                    this.logger.LogInformation("Using {@address} as the donation address for post @{post}", address, post);
                }
                else
                {
                    this.logger.LogCritical("Failed to create address for {@post}, skipping registration.", post);
                    continue;
                }

                var label = $"Post #{post.Number} - {post.Title.Substring(0, Math.Min(post.Title.Length, 30))}...";
                if (await this.TryApplyLabelAddressForPost(post, label, address) is false)
                {
                    this.logger.LogCritical(
                        "Failed to apply label {address_label} to {@address} for {@post}",
                        label,
                        address,
                        post);
                    continue;
                }

                var maybeCommentId = await this.TryCreateDontaionAddressCommentForPostAsync(post, address.Address);
                if (maybeCommentId.TryUnwrapValue(out var commentId) is false)
                {
                    this.logger.LogCritical("Failed to create a donation address comment for {@post}", post);
                    return;
                }

                var bounty = new Bounty(
                    postNumber: post.Number,
                    accountIndex: this.options.WalletAccountIndex,
                    subAddressIndex: address.Index,
                    subAddress: address.Address,
                    commentId: commentId);
                this.context.Bounties.Add(bounty);
                await this.context.SaveChangesAsync();
                await transaction.CommitAsync();

                this.logger.LogInformation("Successfully registered {@post}", post);
            }
            catch (Exception exception)
            {
                this.logger.LogCritical("An unhandled exception occured whilst trying to register {@post}: {error}", post, exception);
                transaction.Rollback();
            }
        }
    }

    private async Task<Option<int>> TryCreateDontaionAddressCommentForPostAsync(Post post, string address, CancellationToken token = default)
    {
        var paymentUrl = $"monero:{address}";
        var qrCode = new PngByteQRCode(GenerateQrCode(paymentUrl, ECCLevel.M));
        var content = $"Donate to the address below to fund this bounty \n" +
            $"[{address}]({paymentUrl}) \n" +
            $"Your donation will be reflected in the comments. \n" +
            $"Payouts will be made once the bounty is complete to the individual(s) who completed the bounty first. \n";
        var attachment = new ImageUpload(
            BlobKey: $"post_{post.Number}",
            Upload: new ImageUploadData(
                FileName: $"post_{post.Number}",
                ContentType: "image/png",
                Content: qrCode.GetGraphic(20)),
            Remove: false);

        var response = await this.fiderApi.PostCommentAsync(post.Number, content, new () { attachment });
        if (response.Error is { } err)
        {
            this.logger.LogError("Failed to create dontaion address comment for {@post}: {@fider_error}", post, err);
            return Option.None<int>();
        }

        this.logger.LogInformation(
            "Successfully created a donation address comment ({comment_id}) {@comment} for post {@post}",
            response.Result,
            new { PaymentUrl = paymentUrl, Content = content, Attachement = attachment },
            post);
        return Option.Some(response.Result);
    }

    private async Task<bool> TryApplyLabelAddressForPost(Post post, string label, (string Address, uint Index) address, CancellationToken token = default)
    {
        var labelAddressRequest = new MoneroRpcRequest(
            "label_address",
            new LabelAddressParameters(new (major: this.options.WalletAccountIndex, minor: address.Index), label: label));
        var labelAddressResponse = await this.walletRpc.JsonRpcAsync<LabelAddressResult>(labelAddressRequest, token);

        if (labelAddressResponse.Error is { } err)
        {
            this.logger.LogCritical(
                "Failed to apply label {label} to {@address} for {@post}",
                label,
                address,
                post);
            return false;
        }

        this.logger.LogInformation(
            "Applied label {label} to {@address} for {@post}",
            label,
            address,
            post);
        return true;
    }

    private async Task<Option<(string Address, uint Index)>> TryCreateAddressForPostAsync(Post post, CancellationToken token = default)
    {
        var accountIndex = this.options.WalletAccountIndex;
        var addressIndex = (uint)post.Number;

        var getAddressRequest = new MoneroRpcRequest(
            "get_address",
            new GetAddressParameters(accountIndex, addressIndex: new () { addressIndex }));
        var getAddressResponse = await this.walletRpc.JsonRpcAsync<GetAddressResult>(getAddressRequest, token);

        if (getAddressResponse.Error is { } getAddressErr)
        {
            this.logger.LogCritical(
                "Failed to retrieve the the address beloning to account #{account_number} at index {address_index} which is the preferred donation address for post {@post}: {@wallet_rpc_error}",
                accountIndex,
                addressIndex,
                post,
                getAddressErr);
            return Option.None<(string Address, uint Index)>();
        }

        if (getAddressResponse.Result is null || getAddressResponse.Result.Addresses?.Count is not > 0)
        {
            this.logger.LogCritical(
                "Failed to retrieve the the address beloning to account #{account_number} at index {address_index} which is the preferred donation address for post {@post} - the RPC server responded but with no result",
                accountIndex,
                addressIndex,
                post);
            return Option.None<(string Address, uint Index)>();
        }

        var preferredAddress = getAddressResponse.Result.Addresses.First();
        var getBalanceRequest = new MoneroRpcRequest(
            "get_balance",
            new GetBalanceParameters(accountIndex, new () { preferredAddress.AddressIndex }, allAccounts: false, strict: true));
        var getBalanceResponse = await this.walletRpc.JsonRpcAsync<GetBalanceResult>(getBalanceRequest, token);

        if (getBalanceResponse.Error is { } getBalanceErr)
        {
            this.logger.LogCritical(
                "Failed to get the balance of the preferred donation address {address} which is required to verify that it is not already in use, so that it can be used as the donation address for {@post}: {@wallet_rpc_error}",
                preferredAddress.Address,
                post,
                getBalanceErr);
            return Option.None<(string Address, uint Index)>();
        }

        var preferredAddressBalance = getBalanceResponse.Result
            ?.PerSubaddress
            ?.SingleOrDefault(i => i.Address == preferredAddress.Address);

        if (preferredAddressBalance is null)
        {
            this.logger.LogCritical(
                "Failed to get the balance of the preferred donation address {address} which is required to verify that it is not already in use, so that it can be used as the donation address for {@post} - the RPC server responded but with no result",
                preferredAddress.Address,
                post);
            return Option.None<(string Address, uint Index)>();
        }

        if (preferredAddressBalance.Balance is > 0)
        {
            this.logger.LogWarning(
                "The preferred donation address {address} for {@post} is already in use with a balance of {@balance}",
                preferredAddress.Address,
                post.Number,
                preferredAddressBalance);
        }
        else
        {
            this.logger.LogInformation(
                "The preferred donation address {address} for {@post} is available",
                preferredAddress.Address,
                post);
            return Option.Some((preferredAddress.Address, preferredAddress.AddressIndex));
        }

        this.logger.LogWarning(
            "The preferred donation address {address} for {@post} is not availlable and so fallback address will be generated for use",
            preferredAddress.Address,
            post);

        /* after we enter the fallback zone we cannot 'cancel' the task, otherwise we risk creating addresses that
         * never get used...
         */
        var createFallbackAddressRequest = new MoneroRpcRequest(
            "create_address",
            new CreateAddressParameters(accountIndex: accountIndex, count: 1, label: string.Empty));
        var createFallbackAddressResponse = await this.walletRpc.JsonRpcAsync<CreateAddressResult>(createFallbackAddressRequest);

        if (createFallbackAddressResponse.Error is { } createFallbackErr)
        {
            this.logger.LogCritical(
                "Failed to create a fallback donation address for {@post}: {@wallet_rpc_error}",
                post.Number,
                createFallbackErr);
        }

        if (createFallbackAddressResponse.Result is null
            || createFallbackAddressResponse.Result.Addresses?.Count is not > 0
            || createFallbackAddressResponse.Result.AddressIndices?.Count is not > 0)
        {
            this.logger.LogCritical("Failed to create a fallback address for {@post} - the RPC server responded but with no result", post);
            return Option.None<(string Address, uint Index)>();
        }

        var fallbackAddress = createFallbackAddressResponse.Result.Addresses.First();
        var fallbackIndex = createFallbackAddressResponse.Result.AddressIndices.First();
        this.logger.LogInformation(
            "Created fallback address {address} at index {index} for {@post}",
            fallbackAddress,
            fallbackIndex,
            post);
        return Option.Some((fallbackAddress, fallbackIndex));
    }

    private async Task<Option<List<Post>>> TryGetPostsToRegisterAsync(CancellationToken token = default)
    {
        var latestBountyTask = BountyQueries.GetLatestBountyAsync(this.context, token);
        var latestPostApiResponseTask = this.fiderApi.GetLatestPostAsync(token);

        var latestBounty = await latestBountyTask;
        var latestPostApiResponse = await latestPostApiResponseTask;

        if (latestPostApiResponse.Error is { } latestPostErr)
        {
            this.logger.LogCritical("Failed to retrieve the latest post: {@fider_error}", latestPostErr);
            return Option.None<List<Post>>();
        }

        if (latestPostApiResponse.Result is null || latestPostApiResponse.Result.Number is 0)
        {
            this.logger.LogInformation("No posts found in Fider");
            return Option.None<List<Post>>();
        }

        var maxRegisteredPostNumber = latestBounty?.PostNumber ?? 0;
        var maxPostNumber = latestPostApiResponse.Result.Number;
        var postsToRegister = maxPostNumber - maxRegisteredPostNumber;

        if (postsToRegister is 0)
        {
            this.logger.LogInformation("Posts up to #{max_registed_post_number} have been registered (there are no more posts to register at this time)", maxRegisteredPostNumber);
            return Option.None<List<Post>>();
        }

        if (postsToRegister is < 0)
        {
            this.logger.LogCritical(
                "Somehow we have registered more posts then there are in Fider. We have registered up to post number {max_registed_post_number}, yet in Fider there we are only up to post number {max_fider_post_number}",
                maxRegisteredPostNumber,
                maxPostNumber);
            return Option.None<List<Post>>();
        }

        if (maxRegisteredPostNumber is 0)
        {
            this.logger.LogInformation("Importing all posts from Fider (initial sync)");
        }
        else
        {
            this.logger.LogTrace("Found {new_posts_count} new posts to register", postsToRegister);
        }

        var fetchPostsApiResponse = await this.fiderApi.GetPostsAsync(count: postsToRegister, token);

        if (fetchPostsApiResponse.Error is { } fetchPostsErr)
        {
            this.logger.LogCritical(
                "Failed fetch the lastest {new_posts_count} posts from Fider: {@fider_error}",
                postsToRegister,
                fetchPostsErr);
            return Option.None<List<Post>>();
        }

        if (fetchPostsApiResponse.Result is null)
        {
            this.logger.LogCritical(
                "Failed fetch the lastest {new_posts_count} posts from Fider - the API returned a result but it was empty",
                postsToRegister);
            return Option.None<List<Post>>();
        }

        var posts = fetchPostsApiResponse.Result;
        if (posts.Count < postsToRegister)
        {
            this.logger.LogWarning("Expected to retrieve {expected_posts_count} posts but only recieved {actual_posts_count}", postsToRegister, posts.Count);
        }
        else
        {
            this.logger.LogTrace("All ({new_posts_count}) new posts recieved for registration", posts.Count);
        }

        return Option.Some(posts.OrderBy(p => p.Number).ToList());
    }

    /// <inheritdoc />
#pragma warning disable SA1202 // Elements should be ordered by access
    public void Dispose()
#pragma warning restore SA1202 // Elements should be ordered by access
    {
        this.timer?.Dispose();
    }
}
