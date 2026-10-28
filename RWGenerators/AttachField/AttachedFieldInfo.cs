namespace RWGenerators.AttachField;

internal record class AttachedFieldInfo(
    string AttachToType,
    string FieldType,
    bool IsValueType,
    string PropertyName,
    ClassInfo ContainingClass
)
{
    public string? DefaultValueString { get; init; } = null;
    public int Impl { get; init; } = 0;
    public bool ClearOnRWGameShutDown { get; init; } = true;
    public bool ClearOnWorldLoaded { get; init; } = false;
}

internal record class ClassInfo(
    string Name,
    string Namespace,
    string Modifiers
)
{ }
