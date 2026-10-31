using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RWGenerators.AttachField;

internal abstract record class BaseAttachedFieldInfo(
    string AttachToType,
    string FieldType,
    bool IsValueType,
    string PropertyName,
    ClassInfo ContainingClass
)
{
    public int Impl { get; init; } = 0;
    public bool ClearOnRWGameShutDown { get; init; } = true;
    public bool ClearOnWorldLoaded { get; init; } = false;
}

internal sealed record class AttachedFieldInfo(
    string AttachToType,
    string FieldType,
    bool IsValueType,
    string PropertyName,
    ClassInfo ContainingClass
) : BaseAttachedFieldInfo(AttachToType, FieldType, IsValueType, PropertyName, ContainingClass)
{
    public string? DefaultValueString { get; init; } = null;
}

internal sealed record class AttachedOptionalFieldInfo(
    string AttachToType,
    string FieldType,
    bool IsValueType,
    string PropertyName,
    ClassInfo ContainingClass
) : BaseAttachedFieldInfo(AttachToType, FieldType, IsValueType, PropertyName, ContainingClass)
{
    public bool AddGetOrCreateValue { get; init; } = false;
    public bool AddGetOrAddValue { get; init; } = false;
}

internal record class ClassInfo(
    string Name,
    string Namespace,
    string Modifiers
)
{
    public static ClassInfo FromSyntax(ClassDeclarationSyntax classSyntax)
    {
        return new ClassInfo(
            classSyntax.Identifier.ToString(),
            classSyntax.GetDeclarationNamespace(),
            classSyntax.Modifiers.ToString()
        );
    }
}
