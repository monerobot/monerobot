namespace MoneroBot.Daemon.Features;

using Microsoft.Extensions.Logging;
using MoneroBot.WalletRpc;
using MoneroBot.WalletRpc.Models;
using MoneroBot.WalletRpc.Models.Generated;

public record Destination(string Address, uint Major, uint Minor);
public record Enote(ulong Amount, string PubKey, bool IsSpent, bool IsUnlocked);
public record Transfer(string TxHash, ulong BlockHeight, Destination Destination, List<Enote> Enotes);

public record GetIncomingTransfers(Destination Destination);

public record IncomingTransfers(List<Transfer> Transfers);

public interface IGetIncomingTransfersHandler
{
    public Task<IncomingTransfers?> HandleAsync(GetIncomingTransfers request, CancellationToken token = default);
}

public class GetIncomingTransfersHandler : IGetIncomingTransfersHandler
{
    private readonly ILogger<GetIncomingTransfersHandler> logger;
    private readonly IWalletRpcClient wallet;

    public GetIncomingTransfersHandler(ILogger<GetIncomingTransfersHandler> logger, IWalletRpcClient wallet)
    {
        this.logger = logger;
        this.wallet = wallet;
    }

    public async Task<IncomingTransfers?> HandleAsync(GetIncomingTransfers request, CancellationToken token = default)
    {
        try
        {
            var response = await this.wallet.JsonRpcAsync<IncomingTransfersResult>(
                new MoneroRpcRequest("incoming_transfers", new IncomingTransfersParameters(
                    transferType: "all",
                    accountIndex: request.Destination.Major,
                    subaddrIndices: new() { request.Destination.Minor })),
                token);

            if (response.Result is not null)
            {
                var transfers = response.Result.Transfers
                    ?.Select(e => new
                    {
                        e.TxHash,
                        e.Pubkey,
                        e.Amount,
                        e.BlockHeight,
                        IsSpent = e.Spent,
                        IsUnlocked = e.Unlocked,
                    })
                    ?.GroupBy(e => new { e.TxHash, e.BlockHeight })
                    ?.Select(g => new Transfer(
                        TxHash: g.Key.TxHash,
                        BlockHeight: g.Key.BlockHeight,
                        Destination: request.Destination,
                        Enotes: g
                            .Select(e => new Enote(
                                Amount: e.Amount,
                                PubKey: e.Pubkey,
                                IsSpent: e.IsSpent,
                                IsUnlocked: e.IsUnlocked))
                            .ToList()))
                    ?.ToList()
                    ?? new();
                return new IncomingTransfers(transfers);
            }

            if (response.Error is { } error)
            {
                this.logger.LogError(
                    "Failed to retrieve incoming transfers for {address}: {@error}",
                    request.Destination.Address,
                    error);
            }
            else if (response.Result is null)
            {
                this.logger.LogError(
                    "Failed to retrieve incoming transfers for {address} - the RPC returned a response but it was empty",
                    request.Destination.Address);
            }
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                "An unhandled exception occured whilst trying to retrieve incoming transfers for {address}: {@exception}",
                request.Destination.Address,
                exception);
        }

        return null;
    }
}
