namespace MoneroBot.Daemon.Features;

using Database;
using Fider;
using Fider.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QRCoder;
using WalletRpc;
using WalletRpc.Models;
using WalletRpc.Models.Generated;
using Db = Database.Entities;
using static QRCoder.QRCodeGenerator;

public record RegisterNewBounty(int PostNumber, uint AccountIndex);

public interface IRegisterNewBountyHandler
{
    public Task<int?> HandleAsync(RegisterNewBounty command, CancellationToken token = default);
}

public class RegisterNewBountyHandler : IRegisterNewBountyHandler
{
    private readonly IDbContextFactory<MoneroBotContext> contextFactory;
    private readonly ILogger<RegisterNewBountyHandler> logger;
    private readonly IFiderApiClient fider;
    private readonly IWalletRpcClient wallet;

    public RegisterNewBountyHandler(
        IDbContextFactory<MoneroBotContext> contextFactory,
        ILogger<RegisterNewBountyHandler> logger,
        IFiderApiClient fider,
        IWalletRpcClient wallet)
    {
        this.contextFactory = contextFactory;
        this.logger = logger;
        this.fider = fider;
        this.wallet = wallet;
    }

    public async Task<int?> HandleAsync(RegisterNewBounty command, CancellationToken token = default)
    {
        this.logger.LogTrace("Attempting to register post #{PostNumber}", command.PostNumber);

        await using var context = await this.contextFactory.CreateDbContextAsync(token);

        Post post;
        try
        {
            post = await this.fider.GetPostAsync(command.PostNumber, token);
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogError(exception, "Failed to fetch post #{PostNumber} using Fider API", command.PostNumber);
            return null;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(token);
        try
        {
            var maybeAddress = await this.TryCreateAddressForPostAsync(context, post, command.AccountIndex, token);
            if (maybeAddress.TryUnwrapValue(out var address))
            {
                this.logger.LogInformation("Using {@Address} as the donation address for post {@Post}", address, post);
            }
            else
            {
                this.logger.LogCritical("Failed to create address for {@Post}, skipping registration", post);
                return null;
            }

            var label = $"Post #{post.Number} - {post.Title.Substring(0, Math.Min(post.Title.Length, 30))}...";
            if (await this.TryApplyLabelAddressForPost(post, label, address, CancellationToken.None) is false)
            {
               this.logger.LogCritical(
                    "Failed to apply label {AddressLabel} to {@Address} for {@Post}",
                    label,
                    address,
                    post);
               return null;
            }

            var maybeComment = await this.TryCreateDonationAddressCommentForPostAsync(post, address.Address);
            if (maybeComment.TryUnwrapValue(out var comment) is false)
            {
                this.logger.LogCritical("Failed to create a donation address comment for {@Post}", post);
                return null;
            }

            var bounty = new Db.Bounty(postNumber: (uint)post.Number, slug: post.Slug)
            {
                DonationAddresses = new List<Db.DonationAddress>
                {
                    new(address.Address, comment.Id),
                },
            };
            context.Bounties.Add(bounty);
            await context.SaveChangesAsync(CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
            this.logger.LogInformation("Successfully registered new {@Post}", post);

            return post.Id;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "An unhandled exception occured whilst trying to register {@Post}", post);
            await transaction.RollbackAsync(CancellationToken.None);
        }

        return null;
    }

    private async Task<Option<(int Id, string Content)>> TryCreateDonationAddressCommentForPostAsync(Post post, string address)
    {
        var paymentUrl = $"monero:{address}";
        var qrCode = new PngByteQRCode(GenerateQrCode(paymentUrl, ECCLevel.M));
        var content = "Donate to the address below to fund this bounty \n" +
            $"[{address}]({paymentUrl}) \n" +
            "Your donation will be reflected in the comments. \n" +
            "Payouts will be made once the bounty is complete to the individual(s) who completed the bounty first. \n";
        var attachment = ImageAttachment.Addition(
            blobKey: $"post_{post.Number}",
            upload: new ImageUploadData(
                FileName: $"post_{post.Number}",
                ContentType: "image/png",
                Content: qrCode.GetGraphic(20)));

        try
        {
            var commentId = await this.fider.PostCommentAsync(
                post.Number,
                content,
                [attachment],
                CancellationToken.None);
            this.logger.LogInformation(
                "Successfully created a donation address comment ({CommentId}) {@Comment} for post {@Post}",
                commentId,
                new { PaymentUrl = paymentUrl, Content = content, Attachement = attachment },
                post);
            return Option.Some((commentId, content));
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogError(exception, "Failed to create donation address comment for {@Post}", post);
            return Option.None<(int, string)>();
        }
    }

    private async Task<bool> TryApplyLabelAddressForPost(Post post, string label, IndexedAddress address, CancellationToken token = default)
    {
        var labelAddressRequest = new MoneroRpcRequest(
            "label_address",
            new LabelAddressParameters(new(major: address.Major, minor: address.Minor), label: label));
        var labelAddressResponse = await this.wallet.JsonRpcAsync<LabelAddressResult>(labelAddressRequest, token);

        if (labelAddressResponse.Error is { } err)
        {
            this.logger.LogError(
                "Failed to apply label {Label} to {@Address} for {@Post}: {@WalletRpcError}",
                label,
                address,
                post,
                err);
            return false;
        }

        this.logger.LogInformation(
            "Applied label {Label} to {@Address} for {@Post}",
            label,
            address,
            post);
        return true;
    }

    private async Task<Option<IndexedAddress>> TryCreateAddressForPostAsync(MoneroBotContext context, Post post, uint accountIndex, CancellationToken token = default)
    {
        /* for now this will often fail until I replicate the functionality of
         * https://github.com/monero-project/monero/pull/6394/files in the wallet rpc client.
         * 
         * The problem at the moment is `get_address` requires the subaddress index be < the number of labeled subaddresses:
         * 
         * wallet2.h: size_t get_num_subaddresses(uint32_t index_major) const { return index_major < m_subaddress_labels.size() ? m_subaddress_labels[index_major].size() : 0; }
         * wallet_rpc_server: THROW_WALLET_EXCEPTION_IF(i >= m_wallet->get_num_subaddresses(req.account_index), error::address_index_outofbound);
         */
        var preferredAddressResult = await this.TryGetUnusedAccountSubaddressAsync(context, post, accountIndex, subaddressIndex: (uint)post.Number, token);
        if (preferredAddressResult.TryUnwrapValue(out var preferred))
        {
            return Option.Some(preferred);
        }

        this.logger.LogWarning(
            "The preferred donation address for post {@Post} could not be determined, or, is not available and so fallback address will be generated for use",
            post);

        /*
        * The scenario where the loop executes multiple times is kind of an edge case. Essentially, if after having registered some bounties
        * (posting QR code and associating an address) you delete the wallet cache and then post a bounty, it will try to re-use address
        * at index 0 (because `create_address` just creates them from the last used index according to the cache). This is an issue because
        * it would result in the address being used for multiple bounties! The database has a `CHECK` to prevent this being saved but best
        * to nip the possibility in the bud here by _verifiying_ the address isn't in use. The problem here is that to get a valid unused address
        * you need to call the `create_address` method `N+1` times where `N` is the number of bounties that were registered. The choice of `N+1`
        * has _some_ logic but I am just adding an upper bound on how many times it tries (a `do { } (while true);` is conceptually the
        * most appropriate - I just didn't want to leave an unbounded loop). If it fails to find an address it will go to the `RegisterNewBounty` branch
        * that logs it can't create an address and then move onto the next bounty. So in this way it will reach a usable bounty address eventually...
        */
        for (var attempt = 1; attempt <= post.Number + 1; attempt++)
        {
            this.logger.LogInformation(
                "Trying to create a donation address for post #{PostNumber} (currently on attempt #{AttemptNumber})",
                post.Number,
                attempt);

            var createAddressRequest = new MoneroRpcRequest(
                "create_address",
                new CreateAddressParameters(accountIndex: accountIndex, count: 1, label: string.Empty));
            var createAddressResponse = await this.wallet.JsonRpcAsync<CreateAddressResult>(createAddressRequest, CancellationToken.None);

            if (createAddressResponse.Error is { } error)
            {
                this.logger.LogCritical(
                    "Failed to create a donation address for {@Post} due to a wallet RPC error: {@WalletRpcError}",
                    post.Number,
                    error);
                return Option.None<IndexedAddress>();
            }

            if (createAddressResponse.Result?.Addresses?.Count is not > 0
                || createAddressResponse.Result.AddressIndices?.Count is not > 0)
            {
                this.logger.LogCritical("Failed to create a dontaion address for {@Post} - the RPC server responded but with no result", post);
                return Option.None<IndexedAddress>();
            }

            var address = createAddressResponse.Result.Addresses.First();
            var index = createAddressResponse.Result.AddressIndices.First();

            var unusedAddressResult = await this.TryGetUnusedAccountSubaddressAsync(context, post, accountIndex, subaddressIndex: index, token);
            if (unusedAddressResult.TryUnwrapValue(out var unusedAdddres))
            {
                this.logger.LogInformation(
                    "Created donation address {Address} for {@Post}",
                    unusedAdddres,
                    post);
                return Option.Some(unusedAdddres);
            }
            else
            {
                this.logger.LogInformation(
                    "Attempt #{AttemptNumber} to create a donation address for post #{PostNumber} did not succeed because the address {Address} is " +
                    "already in use.",
                    attempt,
                    post.Number,
                    address);
            }
        }

        this.logger.LogCritical(
            "All attempts to create a donation address for post #{PostNumber} failed",
            post.Number);
        return Option.None<IndexedAddress>();
    }

    private async Task<Option<IndexedAddress>> TryGetUnusedAccountSubaddressAsync(MoneroBotContext context, Post post, uint accountIndex, uint subaddressIndex, CancellationToken token = default)
    {
        var getAddressRequest = new MoneroRpcRequest(
            "get_address",
            new GetAddressParameters(accountIndex, addressIndex: new List<uint> { subaddressIndex }));
        var getAddressResponse = await this.wallet.JsonRpcAsync<GetAddressResult>(getAddressRequest, token);

        if (getAddressResponse.Error is { } getAddressErr)
        {
            this.logger.LogCritical(
                "Failed to retrieve the the address belonging to account #{AccountNumber} at index {SubaddressIndex} for post #{PostNumber}: {@WalletRpcError}",
                accountIndex,
                subaddressIndex,
                post,
                getAddressErr);
            return Option.None<IndexedAddress>();
        }

        if (getAddressResponse.Result is null || getAddressResponse.Result.Addresses?.Count is not > 0)
        {
            this.logger.LogCritical(
                "Failed to retrieve the the address belonging to account #{AccountNumber} at index {SubaddressIndex} for post {@Post} - the RPC server responded but with no result",
                accountIndex,
                subaddressIndex,
                post);
            return Option.None<IndexedAddress>();
        }

        var address = getAddressResponse.Result.Addresses.First();
        var getBalanceRequest = new MoneroRpcRequest(
            "get_balance",
            new GetBalanceParameters(accountIndex, new HashSet<uint> { address.AddressIndex }, allAccounts: false, strict: true));
        var getBalanceResponse = await this.wallet.JsonRpcAsync<GetBalanceResult>(getBalanceRequest, token);

        if (getBalanceResponse.Error is { } getBalanceErr)
        {
            this.logger.LogCritical(
                "Failed to get the balance of the donation address {@Address} which is required to verify that it is not already in use, so that it can be used as the donation address for {@Post}: {@WalletRpcError}",
                address,
                post,
                getBalanceErr);
            return Option.None<IndexedAddress>();
        }

        var balance = getBalanceResponse.Result
            ?.PerSubaddress
            ?.SingleOrDefault(i => i.Address == address.Address);

        if (balance is null)
        {
            this.logger.LogCritical(
                "Failed to get the balance of the donation address {@Address} which is required to verify that it is not already in use, so that it can be used as the donation address for {@Post} - the RPC server responded but with no result",
                address,
                post);
            return Option.None<IndexedAddress>();
        }

        if (balance.Balance is > 0)
        {
            this.logger.LogWarning(
                "The donation address {@Address} for {@Post} is already in use with a balance of {@Balance}",
                address,
                post,
                balance);
            return Option.None<IndexedAddress>();
        }

        var alreadyTakenByPost = await context.DonationAddresses
            .Where(da => da.Address == address.Address)
            .Select(da => new {da.Bounty!.PostNumber, da.Bounty.Slug})
            .FirstOrDefaultAsync(CancellationToken.None);
        if (alreadyTakenByPost is not null)
        {
            this.logger.LogWarning(
                "The donation address {Address} for post {@Post} is already in use by another post {@OtherPost}",
                address.Address,
                post,
                alreadyTakenByPost);
            return Option.None<IndexedAddress>();
        }

        this.logger.LogInformation(
            "The donation address {Address} for {@Post} is available",
            address.Address,
            post);
        return Option.Some(new IndexedAddress(address.Address, accountIndex, address.AddressIndex));
    }

    private record IndexedAddress(string Address, uint Major, uint Minor);
}
