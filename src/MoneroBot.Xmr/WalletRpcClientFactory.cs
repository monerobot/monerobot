namespace MoneroBot.WalletRpc;

using System.Net;
using Microsoft.Extensions.Options;

public class WalletRpcClientFactory : IWalletRpcClientFactory
{
    private readonly IHttpClientFactory httpFactory;
    private readonly IEnumerable<IValidateOptions<WalletRpcOptions>> validators;

    public WalletRpcClientFactory(IHttpClientFactory httpFactory, IEnumerable<IValidateOptions<WalletRpcOptions>> validators)
    {
        this.httpFactory = httpFactory;
        this.validators = validators;
    }

    public IWalletRpcClient CreateClient(WalletRpcOptions options)
    {
        var errors = this.validators
            .Select(v => v.Validate(string.Empty, options))
            .Where(r => r.Failed)
            .ToList();
        if (errors.Any())
        {
            throw new ArgumentException(nameof(options), string.Join('\n', errors.Select(e => e.FailureMessage)));
        }

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

        var http = new HttpClient(handler)
        {
            BaseAddress = options.BaseAddress!,
        };

        return new WalletRpcClient(http);
    }
}
