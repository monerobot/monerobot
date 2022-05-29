namespace MoneroBot.Database.Entities;

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

[Index(nameof(PostNumber), IsUnique = true)]
public class Bounty
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Bounty() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Bounty(uint postNumber, string slug)
    {
        this.PostNumber = postNumber;
        this.Slug = slug;
    }

    [Key]
    public int Id { get; set; }

    public uint PostNumber { get; set; }

    public string Slug { get; set; }

    public virtual ICollection<DonationAddress>? DonationAddresses { get; set; }
}
