namespace MoneroBot.WalletRpcGenerator
{
    internal class RpcheaderParserResult
    {
        public List<RpcCommand> RpcCommands { get; set; } = new List<RpcCommand>();

        public List<Typedef> Typedefs { get; set; } = new List<Typedef>();

        public List<Structure> Structures { get; set; } = new List<Structure>();
    }
}
