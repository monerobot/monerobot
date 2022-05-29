namespace MoneroBot.WalletRpc
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using MoneroBot.WalletRpc.Models;

#pragma warning disable SA1600 // Elements should be documented
    public interface IWalletRpcClient
#pragma warning restore SA1600 // Elements should be documented
    {
        public Uri? JsonRpcUri { get; }

        public Task<HttpResponseMessage> JsonRpcAsync(MoneroRpcRequest request, CancellationToken token = default);

        public Task<IMoneroRpcResponse<TResult>> JsonRpcAsync<TResult>(MoneroRpcRequest request, CancellationToken token = default)
            where TResult : class;
    }
}
