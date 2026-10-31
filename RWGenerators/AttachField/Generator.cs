using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace RWGenerators.AttachField;

[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(AddEmbeddedSources);

        IncrementalValuesProvider<AttachedFieldInfo> fields = context.SyntaxProvider
            .ForAttributeWithMetadataName($"RWGenerators.AttachFieldAttribute`2",
                (_, _) => true,
                GetAttachedFieldInfo)
            .SelectMany((infos, _) => infos.ImmutableArray);

        context.RegisterSourceOutput(fields, AddAttachedField);

        IncrementalValuesProvider<AttachedOptionalFieldInfo> optionalFields = context.SyntaxProvider
            .ForAttributeWithMetadataName($"RWGenerators.AttachOptionalFieldAttribute`2",
                (_, _) => true,
                GetAttachedOptionalFieldInfo)
            .SelectMany((infos, _) => infos.ImmutableArray);

        context.RegisterSourceOutput(optionalFields, AddAttachedOptionalField);

        context.RegisterSourceOutput(
            fields.Collect()
                .Combine(optionalFields.Collect())
                .Select((p, _) => (EquatableArray<BaseAttachedFieldInfo>)[..p.Left, ..p.Right]),
            AddAttachedFieldManager);
    }

    private void AddEmbeddedSources(IncrementalGeneratorPostInitializationContext ctx)
    {
        ctx.AddEmbeddedAttributeDefinition();
        ctx.AddEmbeddedSource("AttachFieldAttribute.g.cs");
        ctx.AddEmbeddedSource("WeakTable.g.cs");
        ctx.AddEmbeddedSource("ReferenceEqualityComparer.g.cs");
        ctx.AddEmbeddedSource("DictionaryExtensions.g.cs");
    }

    private EquatableArray<AttachedFieldInfo> GetAttachedFieldInfo(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.TargetNode is not ClassDeclarationSyntax classSyntax)
            return [];

        var builder = ImmutableArray.CreateBuilder<AttachedFieldInfo>(ctx.Attributes.Length);

        foreach (AttributeData attributeData in ctx.Attributes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ITypeSymbol attachToType = attributeData.AttributeClass!.TypeArguments[0];
            ITypeSymbol fieldType = attributeData.AttributeClass!.TypeArguments[1];

            (string propertyName, string? defaultValueString) = attributeData.ConstructorArguments switch
            {
                [{ Value: string nameArg }] => (nameArg, null),
                [{ Value: string nameArg }, TypedConstant defaultArg ] => (nameArg, defaultArg.ToCSharpString()),
                _ => throw new ArgumentException("AttachFieldAttribute arguments are invalid")
            };

            builder.Add(new AttachedFieldInfo(
                attachToType.FullyQualifiedName,
                fieldType.FullyQualifiedName,
                fieldType.IsValueType,
                propertyName,
                ClassInfo.FromSyntax(classSyntax)
            )
            {
                DefaultValueString = defaultValueString,
                Impl = attributeData.GetArg("Impl", 0),
                ClearOnRWGameShutDown = attributeData.GetArg("ClearOnRWGameShutDown", true),
                ClearOnWorldLoaded = attributeData.GetArg("ClearOnWorldLoaded", false),
            });
        }

        return new(builder.MoveToImmutable());
    }

    private void AddAttachedField(SourceProductionContext ctx, AttachedFieldInfo info)
    {
        string fileName = $"{info.AttachToType.Replace("global::", "")}_{info.PropertyName}.g.cs";

        ctx.AddSource(fileName, Source.Generate(info));
    }

    private EquatableArray<AttachedOptionalFieldInfo> GetAttachedOptionalFieldInfo(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.TargetNode is not ClassDeclarationSyntax classSyntax)
            return [];

        var builder = ImmutableArray.CreateBuilder<AttachedOptionalFieldInfo>(ctx.Attributes.Length);

        foreach (AttributeData attributeData in ctx.Attributes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ITypeSymbol attachToType = attributeData.AttributeClass!.TypeArguments[0];
            ITypeSymbol fieldType = attributeData.AttributeClass!.TypeArguments[1];

            string propertyName = (string)attributeData.ConstructorArguments[0].Value!;

            builder.Add(new AttachedOptionalFieldInfo(
                attachToType.FullyQualifiedName,
                fieldType.FullyQualifiedName,
                fieldType.IsValueType,
                propertyName,
                ClassInfo.FromSyntax(classSyntax)
            )
            {
                Impl = attributeData.GetArg("Impl", 0),
                ClearOnRWGameShutDown = attributeData.GetArg("ClearOnRWGameShutDown", true),
                ClearOnWorldLoaded = attributeData.GetArg("ClearOnWorldLoaded", false),
                AddGetOrCreateValue = attributeData.GetArg("AddGetOrCreateValue", false),
                AddGetOrAddValue = attributeData.GetArg("AddGetOrAddValue", false),
            });
        }

        return new(builder.MoveToImmutable());
    }

    private void AddAttachedOptionalField(SourceProductionContext ctx, AttachedOptionalFieldInfo info)
    {
        string fileName = $"{info.AttachToType.Replace("global::", "")}_{info.PropertyName}.g.cs";

        ctx.AddSource(fileName, Source.GenerateOptional(info));
    }

    private void AddAttachedFieldManager(SourceProductionContext ctx, EquatableArray<BaseAttachedFieldInfo> infos)
    {
        if (infos.Length == 0)
            return;

        ctx.AddSource("AttachedFieldManager.g.cs", $@"// <auto-generated/>
{string.Join(Utils.Newline, infos
    .Select(i => i.ContainingClass.Namespace)
    .Distinct()
    .Select(n => $"using {n};"))}

namespace RWGenerators;

internal static class AttachedFieldManager
{{
    public static void ApplyHooks()
    {{
        On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
        On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
    }}

    public static void RemoveHooks()
    {{
        On.RainWorldGame.ShutDownProcess -= RainWorldGame_ShutDownProcess;
        On.OverWorld.WorldLoaded -= OverWorld_WorldLoaded;
    }}

    public static void ClearOnRWGameShutDown()
    {{
        {string.Join($"{Utils.Newline}        ", infos
            .Where(i => i.ClearOnRWGameShutDown)
            .Select(ClearMethodCall))}
    }}

    public static void ClearOnWorldLoaded()
    {{
        {string.Join($"{Utils.Newline}        ", infos
            .Where(i => i.ClearOnWorldLoaded)
            .Select(ClearMethodCall))}
    }}

    public static void ClearAllFields()
    {{
        {string.Join($"{Utils.Newline}        ", infos.Select(ClearMethodCall))}
    }}

    private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
    {{
        ClearOnRWGameShutDown();
        orig(self);
    }}

    private static void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self, bool warpUsed)
    {{
        ClearOnWorldLoaded();
        orig(self, warpUsed);
    }}
}}");

        static string ClearMethodCall(BaseAttachedFieldInfo info) => $"{info.AttachToType}.ClearAll{info.PropertyName}Fields();";
    }
}
