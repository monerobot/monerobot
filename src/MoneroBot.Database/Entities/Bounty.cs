namespace MoneroBot.Database.Entities;

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MoneroBot.Database.Attributes;

[Index(nameof(PostNumber), Name = "PostsHaveASingleBounty")]
[Index(nameof(SubAddressIndex), Name = "SubAddressIndexesCannotBeReused")]
[Index(nameof(SubAddress), Name = "SubAddressesCannotBeReused")]
public class Bounty
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Bounty() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Bounty(int postNumber, uint accountIndex, uint subAddressIndex, string subAddress, int commentId)
    {
        this.PostNumber = postNumber;
        this.AccountIndex = accountIndex;
        this.SubAddressIndex = subAddressIndex;
        this.SubAddress = subAddress;
        this.CommentId = commentId;
    }

    [Key]
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int PostNumber { get; set; }

    [Range(0, uint.MaxValue)]
    public uint AccountIndex { get; set; }

    [Range(0, uint.MaxValue)]
    public uint SubAddressIndex { get; set; }

    [Required]
    [MoneroAddress]
    public string SubAddress { get; set; }

    [Range(1, int.MaxValue)]
    public int CommentId { get; set; }

    public virtual List<BountyContribution>? Contributions { get; set; }
}
