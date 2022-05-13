namespace MoneroPay.WalletRpcGenerator
{
    internal record FieldDefinition
    {
        public FieldDefinition(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }

        public string Name { get; set; }
        public string Type { get; set; }
        public string? Default { get; set; }
        public bool Optional { get; set; } = false;
        public bool InKvSerialize { get; set; } = false;
    }
}
