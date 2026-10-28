using RWGenerators;

namespace TestMod;

[AttachField<TestClass, ClassWithBool>("WeakRef")]
[AttachField<TestClass, bool>("WeakValue", false)]
[AttachField<TestClass, bool>("Dict", ClearOnWorldLoaded = true, Impl = AttachFieldImpl.StrongRef)]
[AttachField<TestClass, ClassWithBool>("DictRef", Impl = AttachFieldImpl.StrongRef)]
[AttachField<TestClass, bool>("DictDefTrue", true, Impl = AttachFieldImpl.StrongRef)]
internal static partial class AttachedFields
{
    public static void Test()
    {
        TestClass a = new();
        a.WeakRef.BoolProperty = true;
        a.WeakValue = true;
        a.Dict = true;
        a.DictRef.BoolProperty = true;
        a.DictDefTrue = false;

        Log.Log($"{a.WeakRef} {a.WeakValue} {a.Dict} {a.DictRef} {a.DictDefTrue}");
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
    public override string ToString() => $"ClassWithBool({BoolProperty})";
}
