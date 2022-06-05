namespace MoneroBot.Database.Attributes;

using System.ComponentModel.DataAnnotations;

public class MoneroTransactionIdAttribute : ValidationAttribute
{
    private const string BASE58_ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private const int TX_ID_LENGTH = 65;

    public MoneroTransactionIdAttribute()
        : base($"A monero transaction ids must be {TX_ID_LENGTH} characters long and contain only Base58 characters.")
    { }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return base.IsValid(value)
            && IsCorrectLength(value)
            && IsBase58(value);
    }

    private static bool IsCorrectLength(object value) =>
        value is string address && address.Length == TX_ID_LENGTH;

    private static bool IsBase58(object value) =>
        value is string address && address.All(c => BASE58_ALPHABET.Contains(c));
}
