namespace MoneroBot.WalletRpc
{
    using System.Net.Mime;
    using System.Text;
    using System.Text.Json;
    using MoneroBot.WalletRpc.Models;

    public class WalletRpcClient : IWalletRpcClient
    {
        private readonly HttpClient http;
        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        internal WalletRpcClient(HttpClient http)
        {
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
            var response = await this.JsonRpcAsync(request, token);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStreamAsync(token);
            return await JsonSerializer.DeserializeAsync<MoneroRpcResponse<TResult>>(body, this.jsonOptions, token)
                ?? new MoneroRpcResponse<TResult>(request.Id, request.JsonRpc, default);
        }
    }
}
