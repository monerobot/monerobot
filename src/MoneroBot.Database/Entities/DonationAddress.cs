namespace MoneroBot.Database.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

[Index(nameof(Address), IsUnique = true)]
[Index(nameof(CommentId), IsUnique = true)]
public class DonationAddress
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private DonationAddress() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public DonationAddress(string address, int commentId)
    {
        this.Address = address;
        this.CommentId = commentId;
    }

    [Key]
    public int Id { get; set; }

    public int BountyId { get; set; }

    public virtual Bounty? Bounty { get; set; }
    
    public string Address { get; set; }

    public int CommentId { get; set; }
}

internal class DonationAddressEntityTypeConfiguration : IEntityTypeConfiguration<DonationAddress>
{
    public void Configure(EntityTypeBuilder<DonationAddress> builder)
    {
        builder
            .HasOne(da => da.Bounty)
            .WithMany(b => b.DonationAddresses)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
