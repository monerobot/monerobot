namespace MoneroBot.WalletRpc.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMoneroWalletRpc(this IServiceCollection services)
    {
        services.TryAddSingleton<IValidateOptions<WalletRpcOptions>, WalletRpcOptionsValidator>();
        services.TryAddSingleton<IWalletRpcClientFactory, WalletRpcClientFactory>();
        return services;
    }

    public static IServiceCollection AddMoneroWalletRpcClient(
            this IServiceCollection services,
            Action<WalletRpcOptions>? configure = null,
            string sectionPath = "MoneroWalletRpc")
    {
        services.AddMoneroWalletRpc();

        services
            .AddOptions<WalletRpcOptions>()
            .BindConfiguration(sectionPath)
            .Configure(options => configure?.Invoke(options))
            .ValidateDataAnnotations();

        services.AddTransient(provider =>
        {
            var options = provider.GetRequiredService<IOptions<WalletRpcOptions>>().Value;
            var factory = provider.GetRequiredService<IWalletRpcClientFactory>();
            return factory.CreateClient(options);
        });

        return services;
    }
}
