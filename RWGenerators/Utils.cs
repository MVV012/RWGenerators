using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Text;

namespace RWGenerators;

internal static class Utils
{
    extension(ITypeSymbol typeSymbol)
    {
        public string FullyQualifiedName => typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public static (string Name, string? Namespace) SplitNameAndNamespace(string arg)
    {
        int dotIndex = arg.LastIndexOf('.');
        if (dotIndex == -1)
        {
            return (arg, null);
        }
        else
        {
            return (arg[(dotIndex + 1)..], arg[..dotIndex]);
        }
    }

    extension (IncrementalGeneratorPostInitializationContext ctx)
    {
        public void AddEmbeddedSource(string name)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"{assembly.GetName().Name}.Embedded.{name}";
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            ctx.AddSource(name, SourceText.From(stream, Encoding.UTF8, canBeEmbedded: true));
        }
    }

    extension (BaseTypeDeclarationSyntax syntax)
    {
        // Copied from https://andrewlock.net/creating-a-source-generator-part-5-finding-a-type-declarations-namespace-and-type-hierarchy/#finding-the-namespace-for-a-class-syntax
        // Which is based on https://github.com/dotnet/runtime/blob/25c675ff78e0446fe596cea25c7e3969b0936a33/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen/LoggerMessageGenerator.Parser.cs#L438
        /// <summary>
        /// Determines the namespace the class/enum/struct is declared in, if any
        /// </summary>
        public string GetDeclarationNamespace()
        {
            // If we don't have a namespace at all we'll return an empty string
            // This accounts for the "default namespace" case
            string nameSpace = string.Empty;

            // Get the containing syntax node for the type declaration
            // (could be a nested type, for example)
            SyntaxNode? potentialNamespaceParent = syntax.Parent;

            // Keep moving "out" of nested classes etc until we get to a namespace
            // or until we run out of parents
            while (potentialNamespaceParent != null &&
                    potentialNamespaceParent is not NamespaceDeclarationSyntax
                    && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            // Build up the final namespace by looping until we no longer have a namespace declaration
            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                // We have a namespace. Use that as the type
                nameSpace = namespaceParent.Name.ToString();

                // Keep moving "out" of the namespace declarations until we
                // run out of nested namespace declarations
                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    // Add the outer namespace as a prefix to the final namespace
                    nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                    namespaceParent = parent;
                }
            }

            // return the final namespace
            return nameSpace;
        }
    }
}

internal static class DictionaryExtensions
{
    extension<TKey, TValue>(KeyValuePair<TKey, TValue> kvp)
    {
        public void Deconstruct(out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }

    extension<TKey, TValue>(Dictionary<TKey, TValue> dict) where TValue : new()
    {
        public TValue GetOrCreateValue(TKey key)
        {
            if (!dict.TryGetValue(key, out TValue value))
            {
                value = new TValue();
                dict.Add(key, value);
            }
            return value;
        }
    }
}
