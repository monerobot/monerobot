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

        if (!post.Tags.Contains("approved"))
        {
            this.logger.LogInformation("Post #{PostNumber} doesn't have the 'approved' tag, skipping registration", command.PostNumber);
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
        var attachment = new ImageUpload(
            BlobKey: $"post_{post.Number}",
            Upload: new ImageUploadData(
                FileName: $"post_{post.Number}",
                ContentType: "image/png",
                Content: qrCode.GetGraphic(20)),
            Remove: false);

        try
        {
            var commentId = await this.fider.PostCommentAsync(
                post.Number,
                content,
                new List<ImageUpload> { attachment },
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
        var preferredAddress = await this.TryGetPreferredAddressAsync(context, post, accountIndex, token);
        if (preferredAddress.TryUnwrapValue(out var preferred))
        {
            return Option.Some(preferred);
        }

        this.logger.LogWarning(
            "The preferred donation address ({@Address}) for post {@Post} could not be determined, or, is not available and so fallback address will be generated for use",
            preferred,
            post);

        /* after we enter the fallback zone we cannot 'cancel' the task, otherwise we risk creating addresses that
         * never get used...
         */
        var createFallbackAddressRequest = new MoneroRpcRequest(
            "create_address",
            new CreateAddressParameters(accountIndex: accountIndex, count: 1, label: string.Empty));
        var createFallbackAddressResponse = await this.wallet.JsonRpcAsync<CreateAddressResult>(createFallbackAddressRequest, CancellationToken.None);

        if (createFallbackAddressResponse.Error is { } createFallbackErr)
        {
            this.logger.LogCritical(
                "Failed to create a fallback donation address for {@Post}: {@WalletRpcError}",
                post.Number,
                createFallbackErr);
        }

        if (createFallbackAddressResponse.Result?.Addresses?.Count is not > 0
            || createFallbackAddressResponse.Result.AddressIndices?.Count is not > 0)
        {
            this.logger.LogCritical("Failed to create a fallback address for {@Post} - the RPC server responded but with no result", post);
            return Option.None<IndexedAddress>();
        }

        var fallbackAddress = createFallbackAddressResponse.Result.Addresses.First();
        var fallbackIndex = createFallbackAddressResponse.Result.AddressIndices.First();
        this.logger.LogInformation(
            "Created fallback address {Address} for {@Post}",
            fallbackAddress,
            post);
        return Option.Some(new IndexedAddress(fallbackAddress, accountIndex, fallbackIndex));
    }

    private async Task<Option<IndexedAddress>> TryGetPreferredAddressAsync(MoneroBotContext context, Post post, uint accountIndex, CancellationToken token = default)
    {
        var subaddressIndex = (uint)post.Number;

        var getAddressRequest = new MoneroRpcRequest(
            "get_address",
            new GetAddressParameters(accountIndex, addressIndex: new List<uint> { subaddressIndex }));
        var getAddressResponse = await this.wallet.JsonRpcAsync<GetAddressResult>(getAddressRequest, token);

        if (getAddressResponse.Error is { } getAddressErr)
        {
            this.logger.LogCritical(
                "Failed to retrieve the the address belonging to account #{AccountNumber} at index {SubaddressIndex} which is the preferred donation address for post {@Post}: {@WalletRpcError}",
                accountIndex,
                subaddressIndex,
                post,
                getAddressErr);
            return Option.None<IndexedAddress>();
        }

        if (getAddressResponse.Result is null || getAddressResponse.Result.Addresses?.Count is not > 0)
        {
            this.logger.LogCritical(
                "Failed to retrieve the the address belonging to account #{AccountNumber} at index {SubaddressIndex} which is the preferred donation address for post {@Post} - the RPC server responded but with no result",
                accountIndex,
                subaddressIndex,
                post);
            return Option.None<IndexedAddress>();
        }

        var preferredAddress = getAddressResponse.Result.Addresses.First();
        var getBalanceRequest = new MoneroRpcRequest(
            "get_balance",
            new GetBalanceParameters(accountIndex, new HashSet<uint> { preferredAddress.AddressIndex }, allAccounts: false, strict: true));
        var getBalanceResponse = await this.wallet.JsonRpcAsync<GetBalanceResult>(getBalanceRequest, token);

        if (getBalanceResponse.Error is { } getBalanceErr)
        {
            this.logger.LogCritical(
                "Failed to get the balance of the preferred donation address {@Address} which is required to verify that it is not already in use, so that it can be used as the donation address for {@Post}: {@WalletRpcError}",
                preferredAddress,
                post,
                getBalanceErr);
            return Option.None<IndexedAddress>();
        }

        var preferredAddressBalance = getBalanceResponse.Result
            ?.PerSubaddress
            ?.SingleOrDefault(i => i.Address == preferredAddress.Address);

        if (preferredAddressBalance is null)
        {
            this.logger.LogCritical(
                "Failed to get the balance of the preferred donation address {@Address} which is required to verify that it is not already in use, so that it can be used as the donation address for {@Post} - the RPC server responded but with no result",
                preferredAddress,
                post);
            return Option.None<IndexedAddress>();
        }

        if (preferredAddressBalance.Balance is > 0)
        {
            this.logger.LogWarning(
                "The preferred donation address {@Address} for {@Post} is already in use with a balance of {@Balance}",
                preferredAddress,
                post,
                preferredAddressBalance);
            return Option.None<IndexedAddress>();
        }

        var alreadyTakenByPost = await context.DonationAddresses
            .Where(da => da.Address == preferredAddress.Address)
            .Select(da => new {da.Bounty!.PostNumber, da.Bounty.Slug})
            .FirstOrDefaultAsync(CancellationToken.None);
        if (alreadyTakenByPost is not null)
        {
            this.logger.LogWarning(
                "The preferred donation address {Address} for post {@Post} is already in use by another post {@OtherPost}",
                preferredAddress.Address,
                post,
                alreadyTakenByPost);
            return Option.None<IndexedAddress>();
        }

        this.logger.LogInformation(
            "The preferred donation address {Address} for {@Post} is available",
            preferredAddress.Address,
            post);
        return Option.Some(new IndexedAddress(preferredAddress.Address, accountIndex, preferredAddress.AddressIndex));
    }

    private record IndexedAddress(string Address, uint Major, uint Minor);
}
