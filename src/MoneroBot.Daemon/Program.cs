#pragma warning disable SA1200 // Using directives should be placed correctly
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MoneroBot.Daemon;
using MoneroBot.Daemon.Features;
using MoneroBot.Daemon.Services;
using MoneroBot.Database;
using MoneroBot.Fider.DependencyInjection;
using MoneroBot.WalletRpc.DependencyInjection;
using Serilog;
#pragma warning restore SA1200 // Using directives should be placed correctly

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        builder
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddCommandLine(args);
    })
    .UseSerilog((provider, logging) =>
    {
        logging
            .ReadFrom.Configuration(provider.Configuration)
            .Enrich.FromLogContext();
    })
    .ConfigureServices(services =>
    {
        services
            .AddOptions<DaemonOptions>()
            .BindConfiguration("DaemonOptions")
            .ValidateDataAnnotations();
        services.AddFiderApiClient();
        services.AddMoneroWalletRpcClient();
        services.AddDbContext<MoneroBotContext>(
            (provider, options) =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var env = provider.GetService<IHostEnvironment>();
                options
                    .UseSqlite(config.GetConnectionString("MoneroBotContext"))
                    .UseSnakeCaseNamingConvention()
                    .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: env?.IsDevelopment() is true);
            },
            contextLifetime: ServiceLifetime.Transient);

        services.AddScoped<IGetDonationAddressCommentsHandler, GetDonationAddressCommentsHandler>();
        services.AddScoped<IGetDonationCommentsHandler, GetDonationCommentsHandler>();
        services.AddScoped<IGetAddressIndexHander, GetAddressIndexHandler>();
        services.AddScoped<IGetIncomingTransfersHandler, GetIncomingTransfersHandler>();
        services.AddScoped<IGetUnregisteredPostsHandler, GetUnregisteredPostsHandler>();
        services.AddScoped<IRegisterExistingBountyHandler, RegisterExistingBountyHandler>();
        services.AddScoped<IRegisterNewBountyHandler, RegisterNewBountyHandler>();
        services.AddScoped<IReconcileDonationsWithTransfers, ReconcileDonationsWithTransfersHandler>();

        services.AddHostedService<BountyRegistrationService>();
        services.AddHostedService<BountyDonationService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MoneroBotContext>();
    await db.Database.EnsureCreatedAsync();
}

await host.RunAsync();
