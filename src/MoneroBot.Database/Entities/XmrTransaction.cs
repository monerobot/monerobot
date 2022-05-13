namespace MoneroBot.Database.Entities;

using MoneroBot.Database.Attributes;
using System.ComponentModel.DataAnnotations;

public class XmrTransaction
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private XmrTransaction() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public XmrTransaction(
        string transactionId,
        ulong blockHeight,
        uint accountIndex,
        uint subAddressIndex,
        string subAddress,
        ulong amount,
        bool isSpent,
        bool isUnlocked)
    {
        this.TransactionId = transactionId;
        this.BlockHeight = blockHeight;
        this.AccountIndex = accountIndex;
        this.SubAddressIndex = subAddressIndex;
        this.SubAddress = subAddress;
        this.Amount = amount;
        this.IsSpent = isSpent;
        this.IsUnlocked = isUnlocked;
    }

    [Key]
    [Required]
    [MoneroTransactionId]
    public string TransactionId { get; set; }

    public ulong BlockHeight { get; set; }

    public uint AccountIndex { get; set; }

    [Required]
    [MoneroAddress]
    public string SubAddress { get; set; }

    public uint SubAddressIndex { get; set; }

    public ulong Amount { get; set; }

    public bool IsSpent { get; set; }

    public bool IsUnlocked { get; set; }
}
