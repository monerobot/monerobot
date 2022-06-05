namespace MoneroBot.WalletRpc;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Models;

public interface IWalletRpcClient
{
    public Uri? JsonRpcUri { get; }

    public Task<HttpResponseMessage> JsonRpcAsync(MoneroRpcRequest request, CancellationToken token = default);

    public Task<IMoneroRpcResponse<TResult>> JsonRpcAsync<TResult>(MoneroRpcRequest request, CancellationToken token = default)
        where TResult : class;
}
