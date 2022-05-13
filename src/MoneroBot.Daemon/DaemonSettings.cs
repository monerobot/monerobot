namespace MoneroBot.Daemon;

using System.ComponentModel.DataAnnotations;

public class DaemonOptions
{
    [Required]
    public uint WalletAccountIndex { get; set; }

    [Required]
    public TimeSpan PollingInterval { get; set; }
}
