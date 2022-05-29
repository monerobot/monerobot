namespace MoneroBot.Database;

using Microsoft.EntityFrameworkCore;
using MoneroBot.Database.Entities;
using MoneroBot.Database.Entities.QueryResults;

public class MoneroBotContext : DbContext
{
    public MoneroBotContext(DbContextOptions<MoneroBotContext> options)
        : base(options)
    { }

    public DbSet<Bounty> Bounties { get; set; } = null!;

    public DbSet<Comment> Comments { get; set; } = null!;

    public DbSet<DonationAddress> DonationAddresses { get; set; } = null!;

    public DbSet<Donation> Donations { get; set; } = null!;

    public DbSet<DonationEnote> DonationEnotes { get; set; } = null!;

    public DbSet<Enote> Enotes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MoneroBotContext).Assembly);

        this.RegisterQueryResultEntities(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void RegisterQueryResultEntities(ModelBuilder modelBuilder)
    {
        var types = new[]
        {
            typeof(PostNumber),
        };

        foreach (var type in types)
        {
            modelBuilder.Entity(type).ToTable(type.Name, t => t.ExcludeFromMigrations());
        }
    }
}
