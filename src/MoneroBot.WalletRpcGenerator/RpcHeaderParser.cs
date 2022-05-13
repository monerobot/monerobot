using System.Text.RegularExpressions;

namespace MoneroPay.WalletRpcGenerator
{
    internal static class RpcHeaderParser
    {
        public static readonly Regex RPC_COMMAND_STRUCT_REGEX = new(@"struct COMMAND_RPC_(?<name>[A-Z_]+)(?:\s*//.*)?$");
        public static readonly Regex RPC_REQUEST_STRUCT_REGEX = new(@"struct request_t(: public (?<inherits>[a-z_]+))?(?:\s*//.*)?$");
        public static readonly Regex RPC_RESPONSE_STRUCT_REGEX = new(@"struct response_t(: public (?<inherits>[a-z_]+))?(?:\s*//.*)?$");
        public static readonly Regex STRUCT_REGEX = new(@"struct (?<name>[a-z_]+)(: public (?<inherits>[a-z_]+))?(?:\s*//.*)?$");
        public static readonly Regex KV_SERIALIZE_REQ_REGEX = new(@"KV_SERIALIZE\((?<name>[a-z_]+)\)");
        public static readonly Regex KV_SERIALIZE_OPT_REGEX = new(@"KV_SERIALIZE_OPT\((?<name>[a-z_]+),\s*(?<value>.*)\)");
        public static readonly Regex KV_SERIALIZE_END_REGEX = new(@"END_KV_SERIALIZE_MAP\(\)");
        public static readonly Regex RPC_STRUCT_END_REGEX = new(@"typedef epee::misc_utils::struct_init<[a-z_]+> [a-z_]+");
        public static readonly Regex TYPEDEF_REGEX = new(@"typedef (?<type>.*)\s(?<name>[a-z_]+);(?:\s*//.*)?$");
        public static readonly Regex STRUCT_DECLARATION_REGEX = new(@"^struct (?<type>.*)\s(?<name>[a-z_]+);(?:\s*//.*)?$");
        public static readonly Regex DECLARATION_REGEX = new(@"^(?<type>.*)\s(?<name>[a-z_]+);(?:\s*//.*)?$");

        private delegate ParserState RegexMatchedHandler(ParserState state, Match match, string line, int lineno);
        private static readonly IEnumerable<(Regex, RegexMatchedHandler)> REGEX_HANDLER_PAIRS = new List<(Regex, RegexMatchedHandler)>
        {
            { (RPC_COMMAND_STRUCT_REGEX, HandleCommandMatched) },
            { (KV_SERIALIZE_END_REGEX, HandleKvSerializeEndMatched) },
            { (RPC_STRUCT_END_REGEX, HandleRpcStructEndMatched) },
            { (TYPEDEF_REGEX, HandleTypedefMatched) },
            { (RPC_REQUEST_STRUCT_REGEX, HandleRequestStructMatched) },
            { (RPC_RESPONSE_STRUCT_REGEX, HandleResponseStructMatched) },
            { (STRUCT_REGEX, HandleStructMatched) },
            { (STRUCT_DECLARATION_REGEX, HandleDeclarationMatched) },
            { (DECLARATION_REGEX, HandleDeclarationMatched) },
            { (KV_SERIALIZE_OPT_REGEX, HandleKvSerializeOptMatched) },
            { (KV_SERIALIZE_REQ_REGEX, HandleKvSerializeMatched) }
        };

        public static RpcheaderParserResult ParseHeader(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var state = new ParserState();
            int lineno = 0;
            string? line;
            while (true)
            {
                lineno++;
                if ((line = reader?.ReadLine()) == null) break;
                line = line.Trim();
                
                var handled = false;
                foreach (var (regex, handler) in REGEX_HANDLER_PAIRS)
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;
                    handler(state, match, line, lineno);
                    handled = true;
                    break;
                }

                if (!handled)
                {
                    Console.Error.WriteLine($"Skipping line {lineno}: {line}");
                }
            }

            var conflictingStructures = state.Structures
                .Where(outerStructure => state.Structures
                    .Any(innerStructure =>
                        outerStructure.Name == innerStructure.Name
                        && !outerStructure.Fields.All(
                            outerField => innerStructure.Fields.Any(innerField => innerField == outerField))))
                .ToList();
            foreach (var conflictingStructure in conflictingStructures)
            {
                Console.Error.WriteLine($"The structure {conflictingStructure.Name} was defined multiple times with conflicting field sets");
            }
            if (conflictingStructures.Count > 0)
            {
                throw new Exception($"At least one structure was defined multiple times with conflicting field sets");
            }

            return new RpcheaderParserResult
            {
                RpcCommands = state.RpcCommands.ToList(),
                Structures = state.Structures.GroupBy(s => s.Name).Select(g => g.First()).ToList(),
                Typedefs = state.Typedefs.ToList()
            };
        }

        internal static ParserState HandleCommandMatched(ParserState state, Match match, string line, int lineno)
        {
            var name = match.Groups["name"].Value;
            Console.Error.WriteLine($"I: Detected command '{name}' on line {lineno}: {line}");
            state.RpcCommand = new RpcCommand(requestName: $"{name}_PARAMETERS", responseName: $"{name}_RESULT");
            state.Fieldset = null;
            state.RpcCommands.Add(state.RpcCommand);
            return state;
        }

        internal static ParserState HandleKvSerializeEndMatched(ParserState state, Match match, string line, int lineno)
        {
            Console.Error.WriteLine($"I: Detected end of KV serialize block on line {lineno}: {line}");
            state.Fieldset = null;
            return state;
        }

        internal static ParserState HandleRpcStructEndMatched(ParserState state, Match match, string line, int lineno)
        {
            Console.Error.WriteLine($"I: Detected end of response struct on line {lineno}: {line}");
            state.Fieldset = null;
            return state;
        }

        internal static ParserState HandleTypedefMatched(ParserState state, Match match, string line, int lineno)
        {
            Console.Error.WriteLine($"I: Detected shared typedef on line {lineno}: {line}");
            var type = match.Groups["type"].Value;
            var name = match.Groups["name"].Value;
            state.Typedefs.Add(new Typedef(type: type, alias: name));
            return state;
        }

        internal static ParserState HandleRequestStructMatched(ParserState state, Match match, string line, int lineno)
        {
            Console.Error.WriteLine($"I: Detected start of request struct on line {lineno}: {line}");
            var structure = state.RpcCommand?.RequestStructure;
            if (structure == null) throw new Exception("Expected a request struct to occur in the context of an RPC command but there was none");
            if (match.Groups.TryGetValue("inherits", out var inheritsGroup) && !string.IsNullOrWhiteSpace(inheritsGroup.Value))
            {
                StructInherit(structure, inheritsGroup.Value, state);
            }
            state.Fieldset = structure.Fields;
            return state;
        }

        internal static ParserState HandleResponseStructMatched(ParserState state, Match match, string line, int lineno)
        {
            Console.Error.WriteLine($"I: Detected start of response struct on line {lineno}: {line}");
            var structure = state.RpcCommand?.ResponseStructure;
            if (structure == null) throw new Exception("Expected a response struct to occur in the context of an RPC command but there was none");
            if (match.Groups.TryGetValue("inherits", out var inheritsGroup) && !string.IsNullOrWhiteSpace(inheritsGroup.Value))
            {
                StructInherit(structure, inheritsGroup.Value, state);
            }
            state.Fieldset = structure.Fields;
            return state;
        }

        internal static ParserState HandleStructMatched(ParserState state, Match match, string line, int lineno)
        {
            Console.Error.WriteLine($"I: Detected start of shared struct on line {lineno}: {line}");
            var name = match.Groups["name"].Value;
            var structure = new Structure(name);
            if (match.Groups.TryGetValue("inherits", out var inheritsGroup) && !string.IsNullOrWhiteSpace(inheritsGroup.Value))
            {
                StructInherit(structure, inheritsGroup.Value, state);
            }
            state.Structures.Add(structure);
            state.Fieldset = structure.Fields;
            return state;
        }

        internal static ParserState HandleDeclarationMatched(ParserState state, Match match, string line, int lineno)
        {
            if (state.Fieldset == null)
            {
                Console.Error.WriteLine($"I: Ignoring field declaration on line {lineno} because there is no current field set being built: {line}");
                return state;
            }

            Console.Error.WriteLine($"I: Detected field declaration on line {lineno}: {line}");
            var type = match.Groups["type"].Value;
            var name = match.Groups["name"].Value;
            state.Fieldset.Add(new FieldDefinition(name: name, type: type));
            return state;
        }

        internal static ParserState HandleKvSerializeOptMatched(ParserState state, Match match, string line, int lineno)
        {
            if (state.Fieldset == null)
            {
                throw new Exception("KV_SERIALIZE_OPT should only occur in the context of building a fieldset but none has been detected");
            }

            Console.Error.WriteLine($"I: Detected field serialization default on line {lineno}: {line}");
            var name = match.Groups["name"].Value;
            var value = match.Groups["value"].Value;
            var field = state.Fieldset.FirstOrDefault(f => f.Name == name);
            if (field == null) throw new Exception($"The field {name} was used in a KV_SERIALIZE_OPT but is not present in the current field set context");
            field.Default = value;
            field.Optional = true;
            field.InKvSerialize = true;
            return state;
        }

        internal static ParserState HandleKvSerializeMatched(ParserState state, Match match, string line, int lineno)
        {
            if (state.Fieldset == null)
            {
                throw new Exception("KV_SERIALIZE should only occur in the context of building a fieldset but none has been detected");
            }

            Console.Error.WriteLine($"I: Detected field serialization on line {lineno}: {line}");
            var name = match.Groups["name"].Value;
            var value = match.Groups["value"].Value;
            var field = state.Fieldset.FirstOrDefault(f => f.Name == name);
            if (field == null) throw new Exception($"The field {name} was used in a KV_SERIALIZE_OPT but is not present in the current field set context");
            field.InKvSerialize = true;
            return state;
        }


        internal static void StructInherit(Structure structure, string inheritsName, ParserState state)
        {
            var inheritsStructure = state.Structures.FirstOrDefault(s => s.Name == inheritsName);
            if (inheritsStructure == null) throw new Exception($"The current structure {structure.Name} needs to inherit from {inheritsName} but no structure of that type has been read");
            foreach (var field in inheritsStructure.Fields)
            {
                structure.Fields.Add(field);
            }
        }

        internal class ParserState
        {
            public readonly ICollection<RpcCommand> RpcCommands = new List<RpcCommand>();
            public readonly ICollection<Typedef> Typedefs = new List<Typedef>();
            public readonly ICollection<Structure> Structures = new List<Structure>();
            public RpcCommand? RpcCommand { get; set; }
            public ICollection<FieldDefinition>? Fieldset { get; set; }
        }
    }
}
