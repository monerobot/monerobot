namespace MoneroBot.WalletRpc;

public interface IWalletRpcClientFactory
{
    public IWalletRpcClient CreateClient(WalletRpcOptions options);
}
