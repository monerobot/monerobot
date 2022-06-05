namespace MoneroBot.Database.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

[Index(nameof(CommentId), IsUnique = true)]
public class Donation
{
    public Donation(string txHash, string address, ulong amount, int commentId)
    {
        this.TxHash = txHash;
        this.Address = address;
        this.Amount = amount;
        this.CommentId = commentId;
    }

    [Key]
    public int Id { get; set; }

    public int BountyId { get; set; }

    public virtual Bounty? Bounty { get; set; }
    
    public int CommentId { get; set; }

    [Required]
    public string TxHash { get; set; }
    
    public string Address { get; set; }
    
    public ulong Amount { get; set; }
}

internal class DonationEntityTypeConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder
            .HasOne(d => d.Bounty)
            .WithMany(da => da.Donations)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
