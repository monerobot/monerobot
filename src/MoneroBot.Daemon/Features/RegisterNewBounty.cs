namespace MoneroBot.Daemon.Features;

using Microsoft.Extensions.Logging;
using MoneroBot.Database;
using Db = Database.Entities;
using MoneroBot.Fider;
using MoneroBot.Fider.Models;
using MoneroBot.WalletRpc;
using MoneroBot.WalletRpc.Models;
using MoneroBot.WalletRpc.Models.Generated;
using QRCoder;
using System.Threading.Tasks;
using static QRCoder.QRCodeGenerator;

public record RegisterNewBounty(int PostNumber, uint AccountIndex);

public interface IRegisterNewBountyHandler
{
    public Task<int?> HandleAsync(RegisterNewBounty command, CancellationToken token = default);
}

public class RegisterNewBountyHandler : IRegisterNewBountyHandler
{
    private readonly MoneroBotContext context;
    private readonly ILogger<RegisterNewBountyHandler> logger;
    private readonly IFiderApiClient fider;
    private readonly IWalletRpcClient wallet;

    public RegisterNewBountyHandler(
        MoneroBotContext context,
        ILogger<RegisterNewBountyHandler> logger,
        IFiderApiClient fider,
        IWalletRpcClient wallet)
    {
        this.context = context;
        this.logger = logger;
        this.fider = fider;
        this.wallet = wallet;
    }

    public async Task<int?> HandleAsync(RegisterNewBounty command, CancellationToken token = default)
    {
        this.logger.LogTrace("Attemping to register post #{post_number}", command.PostNumber);

        Post post;
        try
        {
            post = await this.fider.GetPostAsync(command.PostNumber, token);
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogError("Failed to fetch post #{post_number} using Fider API: {@exception}", command.PostNumber, exception);
            return null;
        }

        using var transaction = await this.context.Database.BeginTransactionAsync(token);
        try
        {
            var maybeAddress = await this.TryCreateAddressForPostAsync(post, command.AccountIndex, token);
            if (maybeAddress.TryUnwrapValue(out var address))
            {
                this.logger.LogInformation("Using {@address} as the donation address for post @{post}", address, post);
            }
            else
            {
                this.logger.LogCritical("Failed to create address for {@post}, skipping registration.", post);
                return null;
            }

            var label = $"Post #{post.Number} - {post.Title.Substring(0, Math.Min(post.Title.Length, 30))}...";
            if (await this.TryApplyLabelAddressForPost(post, label, address) is false)
            {
                this.logger.LogCritical(
                    "Failed to apply label {address_label} to {@address} for {@post}",
                    label,
                    address,
                    post);
                return null;
            }

            var maybeComment = await this.TryCreateDontaionAddressCommentForPostAsync(post, address.Address);
            if (maybeComment.TryUnwrapValue(out var comment) is false)
            {
                this.logger.LogCritical("Failed to create a donation address comment for {@post}", post);
                return null;
            }

            var bounty = new Db.Bounty(postNumber: (uint)post.Number, slug: post.Slug)
            {
                DonationAddresses = new List<Db.DonationAddress>(),
            };
            bounty.DonationAddresses.Add(new Db.DonationAddress(bounty, address.Address)
            {
                Comment = new Db.Comment(comment.Id, comment.Content),
            });
            this.context.Bounties.Add(bounty);
            await this.context.SaveChangesAsync();
            await transaction.CommitAsync();
            this.logger.LogInformation("Successfully registered {@post}", post);

            return post.Id;
        }
        catch (Exception exception)
        {
            this.logger.LogCritical("An unhandled exception occured whilst trying to register {@post}: {error}", post, exception);
            transaction.Rollback();
        }

        return null;
    }

    private async Task<Option<(int Id, string Content)>> TryCreateDontaionAddressCommentForPostAsync(Post post, string address, CancellationToken token = default)
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

        try
        {
            var commentId = await this.fider.PostCommentAsync(post.Number, content, new() { attachment });
            this.logger.LogInformation(
                "Successfully created a donation address comment ({comment_id}) {@comment} for post {@post}",
                commentId,
                new { PaymentUrl = paymentUrl, Content = content, Attachement = attachment },
                post);
            return Option.Some((commentId, content));
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogError("Failed to create dontaion address comment for {@post}: {@fider_error}", post, exception);
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

    private async Task<Option<IndexedAddress>> TryCreateAddressForPostAsync(Post post, uint accountIndex, CancellationToken token = default)
    {
        var addressIndex = (uint)post.Number;

        var getAddressRequest = new MoneroRpcRequest(
            "get_address",
            new GetAddressParameters(accountIndex, addressIndex: new() { addressIndex }));
        var getAddressResponse = await this.wallet.JsonRpcAsync<GetAddressResult>(getAddressRequest, token);

        if (getAddressResponse.Error is { } getAddressErr)
        {
            this.logger.LogCritical(
                "Failed to retrieve the the address beloning to account #{account_number} at index {address_index} which is the preferred donation address for post {@post}: {@wallet_rpc_error}",
                accountIndex,
                addressIndex,
                post,
                getAddressErr);
            return Option.None<IndexedAddress>();
        }

        if (getAddressResponse.Result is null || getAddressResponse.Result.Addresses?.Count is not > 0)
        {
            this.logger.LogCritical(
                "Failed to retrieve the the address beloning to account #{account_number} at index {address_index} which is the preferred donation address for post {@post} - the RPC server responded but with no result",
                accountIndex,
                addressIndex,
                post);
            return Option.None<IndexedAddress>();
        }

        var preferredAddress = getAddressResponse.Result.Addresses.First();
        var getBalanceRequest = new MoneroRpcRequest(
            "get_balance",
            new GetBalanceParameters(accountIndex, new() { preferredAddress.AddressIndex }, allAccounts: false, strict: true));
        var getBalanceResponse = await this.wallet.JsonRpcAsync<GetBalanceResult>(getBalanceRequest, token);

        if (getBalanceResponse.Error is { } getBalanceErr)
        {
            this.logger.LogCritical(
                "Failed to get the balance of the preferred donation address {address} which is required to verify that it is not already in use, so that it can be used as the donation address for {@post}: {@wallet_rpc_error}",
                preferredAddress.Address,
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
                "Failed to get the balance of the preferred donation address {address} which is required to verify that it is not already in use, so that it can be used as the donation address for {@post} - the RPC server responded but with no result",
                preferredAddress.Address,
                post);
            return Option.None<IndexedAddress>();
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
            return Option.Some(new IndexedAddress(preferredAddress.Address, addressIndex, preferredAddress.AddressIndex));
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
        var createFallbackAddressResponse = await this.wallet.JsonRpcAsync<CreateAddressResult>(createFallbackAddressRequest);

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
            return Option.None<IndexedAddress>();
        }

        var fallbackAddress = createFallbackAddressResponse.Result.Addresses.First();
        var fallbackIndex = createFallbackAddressResponse.Result.AddressIndices.First();
        this.logger.LogInformation(
            "Created fallback address {address} at index {index} for {@post}",
            fallbackAddress,
            fallbackIndex,
            post);
        return Option.Some(new IndexedAddress(fallbackAddress, accountIndex, fallbackIndex));
    }

    private record IndexedAddress(string Address, uint Major, uint Minor);
}
