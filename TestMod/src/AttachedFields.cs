using RWGenerators;

namespace TestMod;

[AttachField<TestClass, ClassWithBool>("WeakRef")]
[AttachField<TestClass, bool>("WeakValue", false)]
[AttachField<TestClass, bool>("Dict", ClearOnWorldLoaded = true, Impl = AttachFieldImpl.StrongRef)]
[AttachField<TestClass, ClassWithBool>("DictRef", Impl = AttachFieldImpl.StrongRef)]
[AttachField<TestClass, bool>("DictDefTrue", true, Impl = AttachFieldImpl.StrongRef)]
[AttachOptionalField<TestClass, bool>("Optional", AddGetOrCreateValue = true, AddGetOrAddValue = true, ClearOnWorldLoaded = true)]
[AttachOptionalField<TestClass, ClassWithBool>("OptionalRef", AddGetOrAddValue = true, ClearOnWorldLoaded = true)]
[AttachOptionalField<TestClass, bool>("OptionalDict", AddGetOrCreateValue = true, AddGetOrAddValue = true, Impl = AttachFieldImpl.StrongRef)]
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

        a.SetOptional(true);

        if (a.TryGetOptional(out bool value))
        {
            Log.Log($"Optional = {value}");
            a.RemoveOptional();
        }

        a.RemoveOptional();
        a.GetOrAddOptionalRef(() => new());
        a.GetOrCreateOptionalDict();

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
