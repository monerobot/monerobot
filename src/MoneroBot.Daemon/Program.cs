#pragma warning disable SA1200 // Using directives should be placed correctly
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MoneroBot.Daemon;
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
            .Enrich.FromLogContext()
            .WriteTo.Console();
    })
    .ConfigureServices(services =>
    {
        services
            .AddOptions<DaemonOptions>()
            .BindConfiguration("DaemonOptions")
            .ValidateDataAnnotations();
        services.AddFiderApiClient();
        services.AddMoneroWalletRpcClient();
        services.AddDbContextFactory<MoneroBotContext>((provider, options) =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var env = provider.GetService<IHostEnvironment>();
            options
                .UseSqlite(config.GetConnectionString("MoneroBotContext"))
                .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: env?.IsDevelopment() is true);
        });

        services.AddHostedService<BountyRegistrationService>();
        services.AddHostedService<BountyContributionService>();
    })
    .Build();
await host.RunAsync();
