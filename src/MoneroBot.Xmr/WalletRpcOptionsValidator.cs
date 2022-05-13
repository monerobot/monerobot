namespace MoneroBot.WalletRpc;

using System.Text;
using Microsoft.Extensions.Options;

public class WalletRpcOptionsValidator : IValidateOptions<WalletRpcOptions>
{
    public ValidateOptionsResult Validate(string name, WalletRpcOptions options)
    {
        var sb = new StringBuilder();

        if (options.RpcUsername is null && options.RpcPassword is not null)
        {
            sb.AppendLine("If an RPC password is specified so to must a username");
        }

        var errors = sb.ToString();
        if (string.IsNullOrWhiteSpace(errors) is false)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
}
