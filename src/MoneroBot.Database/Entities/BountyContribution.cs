namespace MoneroBot.Database.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneroBot.Database.Attributes;

public class BountyContribution
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private BountyContribution() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public BountyContribution(Bounty bounty, XmrTransaction transaction, int commentId)
    {
        this.Transaction = transaction;
        this.TransactionId = transaction.TransactionId;
        this.Bounty = bounty;
        this.BountyId = bounty.Id;
        this.CommentId = commentId;
    }

    [Key]
    public int Id { get; set; }

    [Required]
    [MoneroTransactionId]
    [ForeignKey(nameof(Transaction))]
    public string TransactionId { get; set; }

    public virtual XmrTransaction? Transaction { get; set; }

    [Required]
    [ForeignKey(nameof(Bounty))]
    public int BountyId { get; set; }

    public virtual Bounty? Bounty { get; set; }

    [Range(1, int.MaxValue)]
    public int CommentId { get; set; }
}

internal class BountyContributionEntityTypeConfiguration : IEntityTypeConfiguration<BountyContribution>
{
    public void Configure(EntityTypeBuilder<BountyContribution> builder)
    {
        builder
            .HasOne(c => c.Bounty)
            .WithMany(b => b.Contributions)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
