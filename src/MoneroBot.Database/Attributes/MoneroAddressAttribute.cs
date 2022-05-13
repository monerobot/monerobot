namespace MoneroBot.Database.Attributes;

using System.ComponentModel.DataAnnotations;

public class MoneroAddressAttribute : ValidationAttribute
{
    private const string BASE58_ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private const int ADDRESS_LENGTH = 95;

    public MoneroAddressAttribute()
        : base($"A monero address must be {ADDRESS_LENGTH} characters long and contain only Base58 characters.")
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

    public override string FormatErrorMessage(string name)
    {
        return base.FormatErrorMessage(name);
    }

    private static bool IsCorrectLength(object value) =>
        value is string address && address.Length == ADDRESS_LENGTH;

    private static bool IsBase58(object value) =>
        value is string address && address.All(c => BASE58_ALPHABET.Contains(c));
}
