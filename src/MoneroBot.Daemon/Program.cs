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
using MoneroBot.WalletRpc;
using Serilog;
#pragma warning restore SA1200 // Using directives should be placed correctly

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, builder) =>
    {
        builder
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true)
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
        services.AddDbContextFactory<MoneroBotContext>(
            (provider, options) =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var env = provider.GetRequiredService<IHostEnvironment>();
                options
                    .UseSqlite(config.GetConnectionString("MoneroBotContext"))
                    .UseSnakeCaseNamingConvention()
                    .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: env.IsDevelopment());
            });

        services.AddTransient<IGetAddressIndexHandler, GetAddressIndexHandler>();
        services.AddTransient<IGetDonationAddressCommentsHandler, GetDonationAddressCommentsHandler>();
        services.AddTransient<IGetDonationCommentsHandler, GetDonationCommentsHandler>();
        services.AddTransient<IGetIncomingTransfersHandler, GetIncomingTransfersHandler>();
        services.AddTransient<IGetUnregisteredPostsHandler, GetUnregisteredPostsHandler>();
        services.AddTransient<IRegisterExistingBountyHandler, RegisterExistingBountyHandler>();
        services.AddTransient<IRegisterNewBountyHandler, RegisterNewBountyHandler>();
        services.AddTransient<ISynchronizePostDonationCommentsHandler, SynchronizePostDonationCommentsHandler>();
        services.AddTransient<IApprovalCommentFeature, AwaitingCommentFeature>();

        services.AddHostedService<BountyRegistrationService>();
        services.AddHostedService<BountyDonationService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MoneroBotContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

await host.RunAsync();
