namespace MoneroBot.WalletRpc;

using System.Diagnostics;
using Models;
using Models.Generated;

public static class WalletRpcClientExtensions
{
    public static async Task<bool> IsRpcServerHealthy(this IWalletRpcClient client, TimeSpan timeout, TimeSpan? delay = null)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                await client.JsonRpcAsync(new MoneroRpcRequest("get_address", new GetAddressParameters(0, new())));
                return true;
            }
            catch
            { }

            await Task.Delay(delay ?? TimeSpan.FromMilliseconds(100));
        }

        return false;
    }
}
