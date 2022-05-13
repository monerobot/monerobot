namespace MoneroBot.WalletRpc.Models
{
    public interface IMoneroRpcResponse<TResult>
        where TResult : class
    {
        public MoneroRpcError? Error { get; set; }

        public string Id { get; set; }

        public string JsonRpc { get; set; }

        public TResult? Result { get; set; }
    }
}
