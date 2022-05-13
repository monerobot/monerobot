namespace MoneroPay.WalletRpcGenerator
{
    internal record RpcCommand
    {
        public RpcCommand(string requestName, string responseName)
        {
            this.RequestStructure = new Structure(requestName);
            this.ResponseStructure = new Structure(responseName);
        }

        public Structure RequestStructure {get;set;}
        public Structure ResponseStructure {get;set;}
    }
}
