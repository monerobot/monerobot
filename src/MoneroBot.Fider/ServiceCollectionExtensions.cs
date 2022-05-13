namespace MoneroBot.Fider.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFiderApiClient(this IServiceCollection services, Action<FiderApiClientOptions>? configure = null)
    {
        services
            .AddOptions<FiderApiClientOptions>()
            .BindConfiguration("Fider")
            .Configure(options => configure?.Invoke(options))
            .ValidateDataAnnotations();

        services.AddHttpClient<IFiderApiClient, FiderApiClient>("Fider", (provider, http) =>
        {
            var options = provider.GetRequiredService<IOptions<FiderApiClientOptions>>().Value;
            if (options.BaseAddress is not null)
            {
                http.BaseAddress = new Uri(options.BaseAddress);
            }

            if (options.ApiKey is not null)
            {
                http.DefaultRequestHeaders.Authorization = new("Bearer", options.ApiKey);
            }

            if (options.ImpersonationUserId is { } userId)
            {
                http.DefaultRequestHeaders.Add("X-Fider-UserID", userId);
            }
        });

        return services;
    }
}
