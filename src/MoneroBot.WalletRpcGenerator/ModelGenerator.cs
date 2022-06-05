namespace MoneroBot.WalletRpcGenerator;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

internal delegate TypeSyntax? TypeSyntaxResolver(string typename, Func<string, TypeSyntax> next);

internal delegate EqualsValueClauseSyntax? EqualsValueClauseResolver(string expression);

internal static class ModelGenerator
{
    // ReSharper disable once InconsistentNaming
    private static readonly HashSet<string> RESERVED_WORDS = new()
    {
        "in",
        "out",
    };

    private static string EscapeReservedWord(string name) => RESERVED_WORDS.Contains(name) ? $"@{name}" : name;

    private static string PascalifyName(string name) => EscapeReservedWord(name
        .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(p => p.ToLower())
        .Select(p => char.ToUpper(p[0]) + p[1..])
        .Aggregate(string.Empty, (acc, p) => $"{acc}{p}"));

    private static string CamelifyName(string name) => EscapeReservedWord(name
        .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(p => p.ToLower())
        .Select((p, i) => i == 0 ? p : char.ToUpper(p[0]) + p[1..])
        .Aggregate(string.Empty, (acc, p) => $"{acc}{p}"));

    private static TypeSyntaxResolver CreateTypeSyntaxRegexResolver(Regex regex, Func<Match, Func<string, TypeSyntax>, TypeSyntax> resolver)
    {
        return (name, nextResolver) =>
        {
            var match = regex.Match(name);
            if (!match.Success) return default;
            return resolver(match, nextResolver);
        };
    }

    private static List<TypeSyntaxResolver> GetDefaultTypeSyntaxResolvers()
    {
        return new()
        {
            {
                CreateTypeSyntaxRegexResolver(
                    new Regex("std::list<(?<type>.*)>"),
                    (m, next) =>
                    {
                        var itemTypeText = m.Groups["type"].Value;
                        var itemTypeSyntax = next(itemTypeText);
                        return
                            NullableType(
                                GenericName(
                                    Identifier("List"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList(
                                            itemTypeSyntax))));
                    }
                )
            },
            {
                CreateTypeSyntaxRegexResolver(
                    new Regex("std::vector<(?<type>.*)>"),
                    (m, next) =>
                    {
                        var itemTypeText = m.Groups["type"].Value;
                        var itemTypeSyntax = next(itemTypeText);
                        if (itemTypeSyntax == null) throw new Exception($"Could not resolve a type syntax for type {itemTypeText}");
                        return
                            NullableType(
                                GenericName(
                                    Identifier("List"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList(
                                            itemTypeSyntax))));
                    }
                )
            },
            {
                CreateTypeSyntaxRegexResolver(
                    new Regex("std::set<(?<type>.*)>"),
                    (m, next) =>
                    {
                        var itemTypeText = m.Groups["type"].Value;
                        var itemTypeSyntax = next(itemTypeText);
                        if (itemTypeSyntax == null) throw new Exception($"Could not resolve a type syntax for type {itemTypeText}");
                        return
                            NullableType(
                                GenericName(
                                    Identifier("HashSet"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList(
                                            itemTypeSyntax))));
                    }
                )
            },
            { CreateTypeSyntaxRegexResolver(new Regex("std::string"), (_, _) => IdentifierName("string")) },
            { CreateTypeSyntaxRegexResolver(new Regex("unsigned"), (_, _) => IdentifierName("uint")) },
            { CreateTypeSyntaxRegexResolver(new Regex("bool"), (_, _) => IdentifierName("bool")) },
            { CreateTypeSyntaxRegexResolver(new Regex("uint64_t"), (_, _) => IdentifierName("ulong")) },
            { CreateTypeSyntaxRegexResolver(new Regex("uint32_t"), (_, _) => IdentifierName("uint")) },
            { CreateTypeSyntaxRegexResolver(new Regex("uint16_t"), (_, _) => IdentifierName("ushort")) },
            { CreateTypeSyntaxRegexResolver(new Regex("uint8_t"), (_, _) => IdentifierName("ubyte")) },
            { CreateTypeSyntaxRegexResolver(new Regex("int64_t"), (_, _) => IdentifierName("long")) },
            { CreateTypeSyntaxRegexResolver(new Regex("int32_t"), (_, _) => IdentifierName("int")) },
            { CreateTypeSyntaxRegexResolver(new Regex("int16_t"), (_, _) => IdentifierName("short")) },
            { CreateTypeSyntaxRegexResolver(new Regex("int8_t"), (_, _) => IdentifierName("byte")) },
        };
    }

    private static TypeSyntaxResolver GetStructureTypeSyntaxResolver(IEnumerable<Structure> structures)
    {
        var nameToStructure = structures
            .ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
        return (name, _) =>
        {
            if (nameToStructure.TryGetValue(name, out var structure)) return IdentifierName(PascalifyName(structure.Name));
            return default;
        };
    }

    private static TypeSyntaxResolver GetTypedefTypeSyntaxResolver(IEnumerable<Typedef> typedefs)
    {
        var aliasToType = typedefs.ToDictionary(td => td.Alias, td => td.Type, StringComparer.OrdinalIgnoreCase);
        return (name, next) =>
        {
            if (aliasToType.TryGetValue(name, out var underlyingTypeName)) return next(underlyingTypeName);
            return default;
        };
    }

    public static Func<string, TypeSyntax> GetFieldTypeTypeSyntaxResolver(IEnumerable<Typedef> typedefs, IEnumerable<Structure> structures)
    {
        var defaultResolvers = GetDefaultTypeSyntaxResolvers();
        var structuresResolver = GetStructureTypeSyntaxResolver(structures);
        var typedefResolver = GetTypedefTypeSyntaxResolver(typedefs);

        TypeSyntax Resolver(string typename)
        {
            Console.Error.WriteLine($"Resolving {typename}");
            foreach (var defaultResolver in defaultResolvers)
            {
                if (defaultResolver.Invoke(typename, Resolver) is { } @default) return @default;
            }
            if (structuresResolver(typename, Resolver) is { } @struct) return @struct;
            if (typedefResolver(typename, Resolver) is { } typedef) return typedef;
            throw new Exception($"Unable to resolve type syntax for type name {typename}");
        }
        return Resolver;
    }

    private static EqualsValueClauseResolver CreateEqualsValueClauseSyntaxRegexResolver(Regex regex, Func<Match, EqualsValueClauseSyntax> resolver)
    {
        return name =>
        {
            var match = regex.Match(name);
            return !match.Success ? default : resolver(match);
        };
    }

    private static List<EqualsValueClauseResolver> GetDefaultEqualsValueClauseResolvers()
    {
        var CRYPTONOTE_MAX_BLOCK_NUMBER = 500_000_000;
        return new()
        {
            CreateEqualsValueClauseSyntaxRegexResolver(
                new Regex(@"\(std::string\)\s*""(?<text>.*)"""),
                m => EqualsValueClause(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(m.Groups["text"].Value)))),
            CreateEqualsValueClauseSyntaxRegexResolver(
                new Regex(@"\(u?int\d+_t\)\s*(?<n>\d+)"),
                m => EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(int.Parse(m.Groups["n"].Value))))),
            CreateEqualsValueClauseSyntaxRegexResolver(
                new Regex(@"\((?<ntype>uint\d+_t)\)\s*CRYPTONOTE_MAX_BLOCK_NUMBER"),
                _ => EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(CRYPTONOTE_MAX_BLOCK_NUMBER)))),
            CreateEqualsValueClauseSyntaxRegexResolver(
                new Regex(@"(?<n>[01])[uU]"),
                m => EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(int.Parse(m.Groups["n"].Value))))),
            CreateEqualsValueClauseSyntaxRegexResolver(
                new Regex(@"(?<bool>true|false)"),
                m => EqualsValueClause(m.Groups["bool"].Value switch
                {
                    "true" => LiteralExpression(SyntaxKind.TrueLiteralExpression),
                    "false" => LiteralExpression(SyntaxKind.FalseLiteralExpression),
                    _ => throw new Exception($"Unrecognised boolean value {m.Groups["bool"].Value}")
                }))
        };
    }


    public static Func<string, EqualsValueClauseSyntax> GetDefaultEqualsValueClauseSyntaxResolver()
    {
        var resolvers = GetDefaultEqualsValueClauseResolvers();
        return defaultText =>
        {
            Console.Error.WriteLine($"Resolving {defaultText} as default value");
            foreach (var resolver in resolvers)
            {
                if (resolver(defaultText) is { } result) return result;
            }
            throw new Exception($"Unable to resolve {defaultText} as a default value");
        };
    }

    public static ClassDeclarationSyntax CreateClass(
            string name,
            ICollection<FieldDefinition> fields,
            Func<string, TypeSyntax?> getTypeSyntaxForField,
            Func<string, EqualsValueClauseSyntax?> getEqualsValueClauseForDefault)
    {
        // TODO(HH): Need to make all reference types have ? if no default

        var @class = ClassDeclaration(Identifier(PascalifyName(name)));
        // Add the public modifier: (public class X)
        @class = @class.AddModifiers(Token(SyntaxKind.PublicKeyword));

        var constructorParameters = new List<ParameterSyntax>();

        // Add the properties for each field
        foreach (var field in fields)
        {
            if (!field.InKvSerialize)
            {
                Console.Error.WriteLine($"I: Skipping field {field.Name} of structure {name} because it was not detected in KV_SERIALIZE");
                continue;
            }

            var attributeList =
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(
                            IdentifierName("JsonPropertyName"))
                        .WithArgumentList(
                            AttributeArgumentList(
                                SingletonSeparatedList(
                                    AttributeArgument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(field.Name))))))));
            var fieldTypeSyntax = getTypeSyntaxForField(field.Type);
            if (fieldTypeSyntax == null) throw new Exception($"Unable to determine type syntax for field type {field.Type}");
            var property =
                PropertyDeclaration(
                    fieldTypeSyntax,
                    Identifier(PascalifyName(field.Name)))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    AccessorList(
                        List(
                            new[] {
                                AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(
                                    Token(SyntaxKind.SemicolonToken)),
                                AccessorDeclaration(
                                    SyntaxKind.SetAccessorDeclaration)
                                .WithSemicolonToken(
                                    Token(SyntaxKind.SemicolonToken))
                            }
                        )
                    )
                )
                .WithAttributeLists(SingletonList(attributeList));

            var constructorParameter = Parameter(Identifier(CamelifyName(field.Name)))
                .WithType(fieldTypeSyntax);
            if (!string.IsNullOrWhiteSpace(field.Default))
            {
                constructorParameter = constructorParameter.WithDefault(getEqualsValueClauseForDefault(field.Default));
            }
            constructorParameters.Add(constructorParameter);

            @class = @class.AddMembers(property);
        }

        @class = @class
            .AddMembers(
                ConstructorDeclaration(
                    Identifier(PascalifyName(name)))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SeparatedList<ParameterSyntax>(
                            constructorParameters
                                .SelectMany((parameterSyntax, i) =>
                                    new SyntaxNodeOrToken[]
                                    {
                                        parameterSyntax,
                                        i == constructorParameters.Count - 1 ? null : Token(SyntaxKind.CommaToken)
                                    })
                                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                                .Where(t => t != null))))
                .WithBody(
                    Block(
                        fields.Select(field =>
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName(PascalifyName(field.Name))),
                                    IdentifierName(CamelifyName(field.Name))))))));

        return @class;
    }
}
