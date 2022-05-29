namespace MoneroBot.WalletRpc
{
    using System.Net.Mime;
    using System.Text;
    using System.Text.Json;
    using Microsoft.Extensions.Logging;
    using MoneroBot.WalletRpc.Models;

    public class WalletRpcClient : IWalletRpcClient
    {
        private readonly ILogger<WalletRpcClient> logger;
        private readonly HttpClient http;
        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public WalletRpcClient(ILogger<WalletRpcClient> logger, HttpClient http)
        {
            this.logger = logger;
            this.http = http;
        }

        public Uri? JsonRpcUri => this.http.BaseAddress;

        public async Task<HttpResponseMessage> JsonRpcAsync(MoneroRpcRequest request, CancellationToken token = default)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request, this.jsonOptions),
                Encoding.UTF8,
                MediaTypeNames.Application.Json);
            return await this.http.PostAsync("/json_rpc", content, token);
        }

        public async Task<IMoneroRpcResponse<TResult>> JsonRpcAsync<TResult>(MoneroRpcRequest request, CancellationToken token = default)
            where TResult : class
        {
            HttpResponseMessage? response;

            try
            {
                response = await this.JsonRpcAsync(request, token);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException error) when (error.StatusCode is null)
            {
                this.logger.LogWarning(
                    "A connection to the Monero Wallet RPC server at {rpc_base_address} count not be established: {message}",
                    this.http.BaseAddress,
                    error.Message);
                return new MoneroRpcResponse<TResult>(request.Id, request.JsonRpc, null)
                {
                    Error = new(-1, error.Message),
                };
            }
            catch (HttpRequestException error)
            {
                this.logger.LogWarning(
                    "A request to the Monero Wallet RPC server at {rpc_base_address} for command {command_name} failed: ({code}) {message}",
                    this.http.BaseAddress,
                    request.Method,
                    error.StatusCode,
                    error.Message);
                return new MoneroRpcResponse<TResult>(request.Id, request.JsonRpc, null)
                {
                    Error = new(-1, error.Message),
                };
            }

            var body = await response.Content.ReadAsStreamAsync(token);
            return await JsonSerializer.DeserializeAsync<MoneroRpcResponse<TResult>>(body, this.jsonOptions, token)
                ?? new MoneroRpcResponse<TResult>(request.Id, request.JsonRpc, default);
        }
    }
}
