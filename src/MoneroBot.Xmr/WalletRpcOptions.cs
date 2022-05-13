namespace MoneroBot.WalletRpc;

using System.ComponentModel.DataAnnotations;

public class WalletRpcOptions
{
    [Required]
    public Uri? BaseAddress { get; set; }

    public string? RpcUsername { get; set; }

    public string? RpcPassword { get; set; }

    public bool? AcceptSelfSignedCerts { get; set; }
}
