using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using MoneroBot.WalletRpcGenerator;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

const string WALLET_RPC_CMD_DEFS_SRC_URL = "https://raw.githubusercontent.com/monero-project/monero/master/src/wallet/wallet_rpc_server_commands_defs.h";

using var httpClient = new HttpClient();
var sourceCodeResponse = await httpClient.GetAsync(WALLET_RPC_CMD_DEFS_SRC_URL);
sourceCodeResponse.EnsureSuccessStatusCode();
var sourceCode = await sourceCodeResponse.Content.ReadAsStreamAsync();

var result = RpcHeaderParser.ParseHeader(sourceCode);
result.Typedefs.Add(new Typedef("subaddress_index", "cryptonote::subaddress_index"));
result.Structures.Add(new Structure("subaddress_index")
{
    Fields = new()
    {
        new FieldDefinition("major", "uint32_t") { InKvSerialize = true },
        new FieldDefinition("minor", "uint32_t") { InKvSerialize = true }
    }
});

var @namespace = NamespaceDeclaration(IdentifierName("MoneroBot.WalletRpc.Models"))
    .AddUsings(
        UsingDirective(ParseName("System")),
        UsingDirective(ParseName("System.Text.Json.Serialization")),
        UsingDirective(ParseName("System.Collections.Generic")));

var fieldTypeTypeSyntaxResolver = ModelGenerator.GetFieldTypeTypeSyntaxResolver(result.Typedefs, result.Structures);
var defaultEqualsValueClauseResolver = ModelGenerator.GetDefaultEqualsValueClauseSyntaxResolver();
foreach (var structure in result.Structures)
{
    @namespace = @namespace.AddMembers(
        ModelGenerator.CreateClass(structure.Name, structure.Fields, fieldTypeTypeSyntaxResolver, defaultEqualsValueClauseResolver));
}

foreach (var command in result.RpcCommands)
{
    @namespace = @namespace.AddMembers(ModelGenerator.CreateClass(command.RequestStructure.Name, command.RequestStructure.Fields, fieldTypeTypeSyntaxResolver, defaultEqualsValueClauseResolver));
    @namespace = @namespace.AddMembers(ModelGenerator.CreateClass(command.ResponseStructure.Name, command.ResponseStructure.Fields, fieldTypeTypeSyntaxResolver, defaultEqualsValueClauseResolver));
}

@namespace = @namespace.NormalizeWhitespace();

using var workspace = new AdhocWorkspace();
var code = Formatter.Format(@namespace, workspace).ToFullString();
using var file = File.Create("Models.cs");
file.Write(Encoding.UTF8.GetBytes(code));
file.Close();
