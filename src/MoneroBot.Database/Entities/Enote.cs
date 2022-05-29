namespace MoneroBot.Database.Entities;

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

[Index(nameof(PubKey), IsUnique = true)]
[Index(nameof(Address))]
public class Enote
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Enote() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Enote(string address, string pubKey, string txHash)
    {
        this.Address = address;
        this.PubKey = pubKey;
        this.TxHash = txHash;
    }

    [Key]
    public int Id { get; set; }

    [Required]
    public string PubKey { get; set; }

    [Required]
    public string Address { get; set; }

    public string TxHash { get; set; }

    public ulong BlockHeight { get; set; }

    public ulong Amount { get; set; }

    public bool IsSpent { get; set; }

    public bool IsUnlocked { get; set; }
}
