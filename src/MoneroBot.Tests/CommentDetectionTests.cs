namespace MoneroBot.Tests;


using Daemon.Features;

public class DonationAddressTests
{
    [Fact]
    public void Test_Legacy_Donation_Address_Comment_Detected()
    {
        var content =
            "Donate to the address below to fund this bounty\n" +
            "BcitbzJN2fqgagTypARzbvhSqPCMaoiZy1JwX7ermHTK1zApkZvXzGFLjWUAiZnDaQ1FMYmy3Fz32XDjqLHwSzVDQrPzPpv\n" +
            "Your donation will be reflected in the comments.\n" +
            "Payouts will be made once the bounty is complete to the individual(s) who completed the bounty first.";
        Assert.Matches(DonationAddressTextRegexes.LegacyText, content);
    }

    [Fact]
    public void Test_PaymentUrl_Style_Donation_Address_Comment_Detected()
    {
        var content =
            "Donate to the address below to fund this bounty \n[BZGu5Xsp5yLifm5znF8UTrAu9GqJUPzRCYabayUJn1c8WV5gsmiJ5FE9SSKmSEzCgDLzrMdjbfUjNDC6j4QtvfxzNs9MA6F](monero:BZGu5Xsp5yLifm5znF8UTrAu9GqJUPzRCYabayUJn1c8WV5gsmiJ5FE9SSKmSEzCgDLzrMdjbfUjNDC6j4QtvfxzNs9MA6F) " +
            "\nYour donation will be reflected in the comments. \nPayouts will be made once the bounty is complete to the individual(s) who completed the bounty first. \n";
        Assert.Matches(DonationAddressTextRegexes.WithPaymentHref, content);
    }
}
