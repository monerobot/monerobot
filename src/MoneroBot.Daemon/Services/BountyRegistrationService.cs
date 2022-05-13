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
        this.logger.LogInformation("Bounty registration service has started");

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
        catch (Exception error)
        {
            this.logger.LogCritical("Unhandled exception occured whilst performing registrations: {error}", error);
        }
        finally
        {
            await Task.Delay(this.options.PollingInterval);
            this.timer?.Change(0, Timeout.Infinite);
        }
    }

    private async Task PerformRegistrations(CancellationToken token = default)
    {
        this.logger.LogTrace("Performing bountry registration");

        var maybePosts = await this.TryGetPostsToRegisterAsync(token);
        if (maybePosts.TryUnwrapValue(out var posts) is false)
        {
            this.logger.LogTrace("No posts to register, halting registration process");
            return;
        }

        foreach (var post in posts)
        {
            token.ThrowIfCancellationRequested();

            this.logger.LogTrace("Beginning transaction to register post #{number}", post.Number);

            using var transaction = await this.context.Database.BeginTransactionAsync(token);
            try
            {
                var maybeAddress = await this.TryCreateAddressForPostAsync(post, token);
                if (maybeAddress.TryUnwrapValue(out var address))
                {
                    this.logger.LogInformation("Using address ({address}, {index}) for post #{number}", address.Address, address.Index, post.Number);
                }
                else
                {
                    this.logger.LogCritical("Failed to create address for post #{number}, skipping registration.", post.Number);
                    continue;
                }

                if (await this.TryLabelAddressForPost(post, address) is false)
                {
                    this.logger.LogCritical("Failed to apply label to address for post #{number}, skipping registration", post.Number);
                    continue;
                }

                var maybeCommentId = await this.TryPostDontaionCommentForPostAsync(post, address.Address);
                if (maybeCommentId.TryUnwrapValue(out var commentId))
                {
                    this.logger.LogInformation("Successfully posted comment with the donation address for post #{number}", post.Number);
                }
                else
                {
                    this.logger.LogCritical("Failed to post a comment with the dontaion link for post #{number}, skipping registration", post.Number);
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

                this.logger.LogInformation("Registered post #{number}", post.Number);
            }
            catch (Exception error)
            {
                this.logger.LogCritical("Unhandled exception occured while trying to register post #{number}: {error}", post.Number, error);
                transaction.Rollback();
            }
        }
    }

    private async Task<Option<int>> TryPostDontaionCommentForPostAsync(Post post, string address, CancellationToken token = default)
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
            this.logger.LogError("Failed to post dontaion comment for post #{number}", post.Number);
            err.Log(this.logger, LogLevel.Critical);
            return Option.None<int>();
        }

        this.logger.LogInformation("Successfully posted donation comment for post #{number}", post.Number);
        return Option.Some(response.Result);
    }

    private async Task<bool> TryLabelAddressForPost(Post post, (string Address, uint Index) address, CancellationToken token = default)
    {
        var label = $"Post #{post.Number} - {post.Title.Substring(0, Math.Min(post.Title.Length, 30))}...";
        var labelAddressRequest = new MoneroRpcRequest(
            "label_address",
            new LabelAddressParameters(new (major: this.options.WalletAccountIndex, minor: address.Index), label: label));
        var labelAddressResponse = await this.walletRpc.JsonRpcAsync<LabelAddressResult>(labelAddressRequest, token);

        if (labelAddressResponse.Error is { } err)
        {
            this.logger.LogCritical(
                "Failed to apply label '{label}' to address {address} post post #{number}",
                label,
                address.Address,
                post.Number);
            return false;
        }

        this.logger.LogInformation(
            "Applied label '{label}' to address {address} post post #{number}",
            label,
            address.Address,
            post.Number);
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
                "Failed to retrieve the address of account #{account} at index {index} which was to be used for post {number}: ({code}) {message}",
                accountIndex,
                addressIndex,
                post.Number,
                getAddressErr.Code,
                getAddressErr.Message);
            return Option.None<(string Address, uint Index)>();
        }

        if (getAddressResponse.Result is null || getAddressResponse.Result.Addresses?.Count is not > 0)
        {
            this.logger.LogCritical(
                "Failed to retrieve the address of account #{account} at index {index} which was to be used for post {number} - the RPC server responded but with no result",
                accountIndex,
                addressIndex,
                post.Number);
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
                "Failed to get the balance of address {address} which is required to verify it is not in used so that it can be used fort post {number}: ({code}) {message}",
                preferredAddress.Address,
                post.Number,
                getBalanceErr.Code,
                getBalanceErr.Message);
            return Option.None<(string Address, uint Index)>();
        }

        var preferredAddressBalance = getBalanceResponse.Result
            ?.PerSubaddress
            ?.SingleOrDefault(i => i.Address == preferredAddress.Address);

        if (preferredAddressBalance is null)
        {
            this.logger.LogCritical(
                "Failed to get the balance of address {address} which is required to verify it is not in used so that it can be used fort post {number} - the RPC server responded but with no result",
                preferredAddress.Address,
                post.Number);
            return Option.None<(string Address, uint Index)>();
        }

        if (preferredAddressBalance.Balance is > 0)
        {
            this.logger.LogWarning(
                "The preferred address {address} for post #{number} is already in use with a balance of {balance}",
                preferredAddress.Address,
                post.Number,
                preferredAddressBalance.Balance);
        }
        else
        {
            this.logger.LogInformation("The preferred address {address} for post #{number} is available", preferredAddress.Address, post.Number);
            return Option.Some((preferredAddress.Address, preferredAddress.AddressIndex));
        }

        this.logger.LogWarning(
            "The preferred address {address} for post #{number} is not availlable and so a 'random' address will be generated for use",
            preferredAddress.Address,
            post.Number);

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
                "Failed to create a fallback address for post #{number}: ({code}) {message}",
                post.Number,
                createFallbackErr.Code,
                createFallbackErr.Message);
        }

        if (createFallbackAddressResponse.Result is null
            || createFallbackAddressResponse.Result.Addresses?.Count is not > 0
            || createFallbackAddressResponse.Result.AddressIndices?.Count is not > 0)
        {
            this.logger.LogCritical("Failed to create a fallback address for post #{number} - the RPC server responded but with no result", post.Number);
            return Option.None<(string Address, uint Index)>();
        }

        var fallbackAddress = createFallbackAddressResponse.Result.Addresses.First();
        var fallbackIndex = createFallbackAddressResponse.Result.AddressIndices.First();
        this.logger.LogInformation(
            "Created fallback address {address} at index {index} for post #{number}",
            fallbackAddress,
            fallbackIndex,
            post.Number);
        return Option.Some((fallbackAddress, fallbackIndex));
    }

    private async Task<Option<List<Post>>> TryGetPostsToRegisterAsync(CancellationToken token = default)
    {
        var latestBountyTask = BountyQueries.GetLatestBountyAsync(this.context, token);
        var latestPostApiResponseTask = this.fiderApi.GetLatestPostAsync(token);

        var latestBounty = await latestBountyTask;
        var latestPostApiResponse = await latestPostApiResponseTask;

        if (latestPostApiResponse.Error is not null)
        {
            this.logger.LogCritical("Failed to retrieve latest post");
            latestPostApiResponse.Error.Log(this.logger, LogLevel.Critical);
            return Option.None<List<Post>>();
        }

        if (latestPostApiResponse.Result is null || latestPostApiResponse.Result.Number is 0)
        {
            this.logger.LogInformation("No posts found in Fider");
            return Option.None<List<Post>>();
        }

        var maxRegisteredPostNumber = latestBounty?.PostNumber;
        var maxPostNumber = latestPostApiResponse.Result.Number;
        var postsToRegister = maxPostNumber - maxRegisteredPostNumber.GetValueOrDefault(0);

        if (postsToRegister is 0)
        {
            this.logger.LogInformation("All {count} posts in Fider have been registered", maxRegisteredPostNumber);
            return Option.None<List<Post>>();
        }

        if (postsToRegister is < 0)
        {
            this.logger.LogCritical(
                "Somehow we have registered more posts then there are in Fider. We have registered up to post number {registered}, yet in Fider there we are only up to post number {posts}",
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
            this.logger.LogTrace("Found {count} new posts to register", postsToRegister);
        }

        var fetchPostsApiResponse = await this.fiderApi.GetPostsAsync(count: postsToRegister, token);

        if (fetchPostsApiResponse.Error is not null)
        {
            this.logger.LogCritical("Failed to retrieve posts");
            fetchPostsApiResponse.Error.Log(this.logger, LogLevel.Critical);
            return Option.None<List<Post>>();
        }

        if (fetchPostsApiResponse.Result is null)
        {
            this.logger.LogCritical("Failed to retrieve posts");
            return Option.None<List<Post>>();
        }

        var posts = fetchPostsApiResponse.Result;
        if (posts.Count < postsToRegister)
        {
            this.logger.LogWarning("Expected to retrieve {expected} posts but only recieved {actual}", postsToRegister, posts.Count);
        }
        else
        {
            this.logger.LogTrace("All {expected} posts recieved", posts.Count);
        }

        /* for whatever reason we may have entries for post numbers [1, 2, 4, 5, 8], because the post
         * numbers are contiguous and we _should_ be importing them in order we should instead have [1, 2, 3, 4, 5, 6, 7, 8]
         * but if this isn't the case we find the 'missing' post numbers (i.e 3, 6, 7) and grab the individual posts themselves
         * adding them to the list of posts to import.
         *
         * In theory this would allow you to delete a post from the database and have it be successfully restored.
         */
        var missingPostNumbers = await BountyQueries.GetMissingPostNumbers(this.context, token);
        if (missingPostNumbers.Any())
        {
            this.logger.LogWarning(
                "The database contains bounties for post numbers up to #{max}, however it is missing entries for post numbers: {missing}",
                maxRegisteredPostNumber,
                missingPostNumbers);
            this.logger.LogInformation("Attempting to reterive missing posts with numbers: {missing}", missingPostNumbers);
            foreach (var number in missingPostNumbers)
            {
                var getPostResponse = await this.fiderApi.GetPostAsync(number: number, token);
                if (getPostResponse.Error is not null)
                {
                    this.logger.LogCritical("Failed to retrieve post #{number} which is missing from the bounty database, skipping registration", number);
                    getPostResponse.Error.Log(this.logger, LogLevel.Critical);
                    continue;
                }
                else if (getPostResponse.Result is null)
                {
                    this.logger.LogCritical(
                        "Failed to retrieve post #{number} which is missing from the bounty database, skipping registration - the API returned a response but it was empty",
                        number);
                    continue;
                }

                this.logger.LogInformation("Successfully fetched missing post #{number}", number);
                posts.Add(getPostResponse.Result);
            }
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
