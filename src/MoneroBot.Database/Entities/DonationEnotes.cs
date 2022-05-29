namespace MoneroBot.Database.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

[Index(nameof(EnoteId), IsUnique = true)]
public class DonationEnote
{
    private DonationEnote() { }

    public DonationEnote(Donation donation, Enote enote)
    {
        this.Donation = donation;
        this.DonationId = donation.Id;
        this.Enote = enote;
        this.EnoteId = enote.Id;
    }

    [Key]
    public int Id { get; set; }

    public int DonationId { get; set; }

    public virtual Donation? Donation { get; set; }

    public int EnoteId { get; set; }

    public virtual Enote? Enote { get; set; }
}

internal class DonationEnoteEntityTypeConfiguration : IEntityTypeConfiguration<DonationEnote>
{
    public void Configure(EntityTypeBuilder<DonationEnote> builder)
    {
        builder
            .HasOne(de => de.Donation)
            .WithMany(d => d.DonationEnotes)
            .OnDelete(DeleteBehavior.NoAction);
        builder
            .HasOne(de => de.Enote)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);
    }
}
