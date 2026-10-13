using RWGenerators;

namespace TestMod;

[AttachField<TestClass, ClassWithBool>("CWTRef", Impl = AttachFieldImpl.CWT)]
[AttachField<TestClass, bool>("CWTValue", false)]
[AttachField<TestClass, bool>("Dict", ClearOnWorldLoaded = true, Impl = AttachFieldImpl.StrongRefDict)]
internal static partial class AttachedFields
{
    public static void Test()
    {
        TestClass a = new();
        a.CWTValue = true;
        Console.WriteLine(a.CWTValue);
    }
}

public class TestClass
{
    public bool Bool = false;
    public int Int = 0;
}

interface IHaveBool
{
    bool BoolProperty { get; set; }
}

public class ClassWithBool : IHaveBool
{
    public bool BoolField = false;
    public bool BoolProperty
    {
        get => BoolField;
        set => BoolField = value;
    }
}
