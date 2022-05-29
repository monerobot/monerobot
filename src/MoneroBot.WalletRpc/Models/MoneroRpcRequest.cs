namespace MoneroBot.WalletRpc.Models
{
    using System.Text.Json.Serialization;

    public record MoneroRpcRequest
    {
        public const string DEFAULT_ID = "0";
        public const string DEFAULT_JSON_RPC = "2.0";

        public MoneroRpcRequest(string method, object @params)
        {
            this.Method = method;
            this.Params = @params;
        }

        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; init; } = DEFAULT_JSON_RPC;

        [JsonPropertyName("id")]
        public string Id { get; init; } = DEFAULT_ID;

        [JsonPropertyName("method")]
        public string Method { get; init; }

        [JsonPropertyName("params")]
        public object Params { get; init; }
    }

    public record MoneroRpcRequest<TParameters> : MoneroRpcRequest
        where TParameters : notnull
    {
        public MoneroRpcRequest(string method, TParameters @params)
            : base(method, @params)
        { }

        [JsonPropertyName("params")]
        public new TParameters Params => (TParameters)base.Params;
    }
}
