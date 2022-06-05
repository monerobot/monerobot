namespace MoneroBot.WalletRpcGenerator
{
    internal record Typedef
    {
        public Typedef(string type, string alias)
        {
            this.Type = type;
            this.Alias = alias;
        }

        public string Type {get;set;}

        public string Alias {get;set;}
    }
}
