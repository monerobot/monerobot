namespace MoneroBot.Database;

using Microsoft.EntityFrameworkCore;
using Entities;

public class MoneroBotContext : DbContext
{
    public MoneroBotContext(DbContextOptions<MoneroBotContext> options)
        : base(options)
    { }

    public DbSet<Bounty> Bounties { get; set; } = null!;

    public DbSet<DonationAddress> DonationAddresses { get; set; } = null!;

    public DbSet<Donation> Donations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MoneroBotContext).Assembly);

        this.RegisterQueryResultEntities(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void RegisterQueryResultEntities(ModelBuilder modelBuilder)
    {
        var types = Type.EmptyTypes;

        foreach (var type in types)
        {
            modelBuilder.Entity(type).ToTable(type.Name, t => t.ExcludeFromMigrations());
        }
    }
}
