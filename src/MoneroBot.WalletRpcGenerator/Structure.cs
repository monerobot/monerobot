namespace MoneroPay.WalletRpcGenerator
{
    internal record Structure
    {
        public Structure(string name)
        {
            this.Name = name;
        }

        public string Name {get;set;}
        public List<FieldDefinition> Fields {get;set;} = new List<FieldDefinition>();
    }
}
