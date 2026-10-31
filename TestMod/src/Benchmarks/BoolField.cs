using BenchmarkDotNet.Attributes;
using RWGenerators;
using System.Runtime.CompilerServices;

namespace TestMod.Benchmarks;

internal class BoolTestClass
{
    public bool Field = false;
    public bool Property { get; set; }
}

[AttachField<BoolTestClass, bool>("Dict", Impl = AttachFieldImpl.StrongRef)]
[AttachField<BoolTestClass, bool>("Custom", Impl = AttachFieldImpl.WeakRef)]
internal static partial class AttachedBoolFields
{
    private class CWTValueHolder
    {
        public bool Value = default;
    }
    private static ConditionalWeakTable<BoolTestClass, CWTValueHolder> s_CWTFields = new();

    extension(BoolTestClass attachedTo)
    {
        public bool CWT
        {
            get => s_CWTFields.GetValue(attachedTo, (_) => new()).Value;
            set => s_CWTFields.GetValue(attachedTo, (_) => new()).Value = value;
        }
        public static void ClearAllCWTFields() => s_CWTFields = new();
    }
}

public class ReadBool
{
    private BoolTestClass obj = new();

    public int ObjectCount = 100;

    [Params(false, true)]
    public bool Value;

    [Params(1_000_000)]
    public int Iterations;

    [IterationSetup]
    public void Setup()
    {
        for (int i = 0; i < ObjectCount - 1; i++)
        {
            BoolTestClass o = new();
            o.CWT = true;
            o.Custom = true;
            o.Dict = true;
        }
        obj.Custom = Value;
        obj.CWT = Value;
        obj.Property = Value;
        obj.Dict = Value;
    }

    [IterationCleanup]
    public void Cleanup()
    {
        BoolTestClass.ClearAllCustomFields();
        BoolTestClass.ClearAllCWTFields();
        BoolTestClass.ClearAllDictFields();
        GC.Collect();
    }

    [Benchmark]
    public int Custom()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            res += obj.Custom ? 1 : 0;
        }
        return res;
    }

    [Benchmark]
    public int CWT()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            res += obj.CWT ? 1 : 0;
        }
        return res;
    }

    [Benchmark]
    public int Dict()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            res += obj.Dict ? 1 : 0;
        }
        return res;
    }

    [Benchmark]
    public int Property()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            res += obj.Property ? 1 : 0;
        }
        return res;
    }
}

public class SetBool
{
    private BoolTestClass obj = new();

    [Params(1, 10, 100)]
    public int ObjectCount;

    [Params(1_000_000)]
    public int Iterations;

    [IterationSetup]
    public void Setup()
    {
        for (int i = 0; i < ObjectCount - 1; i++)
        {
            BoolTestClass o = new();
            o.CWT = true;
            o.Custom = true;
            o.Dict = true;
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        BoolTestClass.ClearAllCustomFields();
        BoolTestClass.ClearAllCWTFields();
        BoolTestClass.ClearAllDictFields();
        GC.Collect();
    }

    [Benchmark]
    public int Custom()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            obj.Custom = true;
        }
        return res;
    }

    [Benchmark]
    public int CWT()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            obj.CWT = true;
        }
        return res;
    }

    [Benchmark]
    public int Dict()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            obj.Dict = true;
        }
        return res;
    }
}

public class RandomBoolTest
{
    private BoolTestClass[] obj = null!;
    public int[] actions = null!;
    public int[] indexes = null!;

    private WeakReference<BoolTestClass>[] objWeakRef = null!;

    [Params(200)]
    public int ObjCount;

    [Params(10_000_000)]
    public int ActionCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        Random rand = new(1234);

        actions = new int[ActionCount];
        indexes = new int[ActionCount];
        for (int i = 0; i < ActionCount; i++)
        {
            actions[i] = rand.Next(0, 4);
            indexes[i] = rand.Next(0, ObjCount);
        }
    }

    [IterationSetup]
    public void Setup()
    {
        obj = new BoolTestClass[ObjCount];
        objWeakRef = new WeakReference<BoolTestClass>[ObjCount];
        for (int i = 0; i < ObjCount; i++)
        {
            obj[i] = new();
            objWeakRef[i] = new(obj[i]);
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        GC.Collect();
        int alive = objWeakRef.Count(w => w.TryGetTarget(out _));

        Log.Log($"{alive}/{ObjCount} old objects are alive");

        BoolTestClass.ClearAllCustomFields();
        BoolTestClass.ClearAllCWTFields();
        BoolTestClass.ClearAllDictFields();
        GC.Collect();
    }

    [Benchmark]
    public int Custom()
    {
        int res = 0;
        for (int i = 0; i < ActionCount; i++)
        {
            int a = actions[i];
            if (a == 0)
                res += obj[indexes[i]].Custom ? 1 : 0;
            else if (a == 1)
                obj[indexes[i]].Custom = true;
            else if (a == 2)
                obj[indexes[i]].Custom = false;
            else
                obj[indexes[i]] = new();
        }
        return res;
    }

    [Benchmark]
    public int CWT()
    {
        try
        {
            int res = 0;
            for (int i = 0; i < ActionCount; i++)
            {
                int a = actions[i];
                if (a == 0)
                    res += obj[indexes[i]].CWT ? 1 : 0;
                else if (a == 1)
                    obj[indexes[i]].CWT = true;
                else if (a == 2)
                    obj[indexes[i]].CWT = false;
                else
                    obj[indexes[i]] = new();
            }
            return res;
        }
        catch (Exception e)
        {
            Log.LogError(e);
            throw e;
        }
    }

    [Benchmark]
    public int Dict()
    {
        int res = 0;
        for (int i = 0; i < ActionCount; i++)
        {
            int a = actions[i];
            if (a == 0)
                res += obj[indexes[i]].Dict ? 1 : 0;
            else if (a == 1)
                obj[indexes[i]].Dict = true;
            else if (a == 2)
                obj[indexes[i]].Dict = false;
            else
                obj[indexes[i]] = new();
        }
        return res;
    }

    [Benchmark]
    public int Property()
    {
        int res = 0;
        for (int i = 0; i < ActionCount; i++)
        {
            int a = actions[i];
            if (a == 0)
                res += obj[indexes[i]].Property ? 1 : 0;
            else if (a == 1)
                obj[indexes[i]].Property = true;
            else if (a == 2)
                obj[indexes[i]].Property = false;
            else
                obj[indexes[i]] = new();
        }
        return res;
    }
}
