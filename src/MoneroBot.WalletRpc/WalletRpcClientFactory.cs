namespace MoneroBot.WalletRpc;

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class WalletRpcClientFactory : IWalletRpcClientFactory
{
    private readonly ILoggerFactory loggerFactory;
    private readonly IEnumerable<IValidateOptions<WalletRpcOptions>> validators;

    public WalletRpcClientFactory(ILoggerFactory loggerFactory, IEnumerable<IValidateOptions<WalletRpcOptions>> validators)
    {
        this.loggerFactory = loggerFactory;
        this.validators = validators;
    }

    public static HttpClientHandler CreateHttpClientHandler(WalletRpcOptions options)
    {
        var handler = new HttpClientHandler();
        if (options.AcceptSelfSignedCerts is true)
        {
            handler.ServerCertificateCustomValidationCallback += (_, _, _, _) => true;
        }

        if (options.RpcUsername is { } username)
        {
            var credentials = new CredentialCache
            {
                { options.BaseAddress!, "Digest", new NetworkCredential(username, options.RpcPassword) },
            };
            handler.PreAuthenticate = false;
            handler.Credentials = credentials;
        }

        return handler;
    }

    public static HttpClient ConfigureHttpClient(HttpClient http, WalletRpcOptions options)
    {
        http.BaseAddress = options.BaseAddress;
        return http;
    }

    public IWalletRpcClient CreateClient(WalletRpcOptions options)
    {
        var errors = this.validators
            .Select(v => v.Validate(string.Empty, options))
            .Where(r => r.Failed)
            .ToList();
        if (errors.Any())
        {
            throw new ArgumentException(string.Join('\n', errors.Select(e => e.FailureMessage)), nameof(options));
        }

        var handler = CreateHttpClientHandler(options);
        var http = ConfigureHttpClient(new HttpClient(handler), options);
        var logger = this.loggerFactory.CreateLogger<WalletRpcClient>();
        return new WalletRpcClient(logger, http);
    }
}
