namespace MoneroBot.Database.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Index(nameof(CommentId), IsUnique = true)]
public class Donation
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Donation() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Donation(DonationAddress donationAddress)
    {
        this.DonationAddressId = donationAddress.Id;
        this.DonationAddress = donationAddress;
    }

    [Key]
    public int Id { get; set; }

    public int DonationAddressId { get; set; }

    public virtual DonationAddress? DonationAddress { get; set; }

    public virtual ICollection<DonationEnote>? DonationEnotes { get; set; }

    public int? CommentId { get; set; }

    public virtual Comment? Comment { get; set; }
}

internal class DonationEntityTypeConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder
            .HasOne(d => d.DonationAddress)
            .WithMany(da => da.Donations)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
