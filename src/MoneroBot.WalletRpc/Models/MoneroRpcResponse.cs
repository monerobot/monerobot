namespace MoneroBot.WalletRpc.Models;

using System.Text.Json.Serialization;

public class MoneroRpcResponse<TResult> : IMoneroRpcResponse<TResult>
    where TResult : class
{
    public MoneroRpcResponse(string id, string jsonrpc, TResult? result)
    {
        this.Id = id;
        this.JsonRpc = jsonrpc;
        this.Result = result;
    }

    [JsonPropertyName("error")]
    public MoneroRpcError? Error { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; }

    [JsonPropertyName("result")]
    public TResult? Result { get; set; }
}

public class MoneroRpcError
{
    public MoneroRpcError(int code, string message)
    {
        this.Code = code;
        this.Message = message;
    }

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}
