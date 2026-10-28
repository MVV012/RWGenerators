using BenchmarkDotNet.Attributes;
using RWGenerators;
using System.Runtime.CompilerServices;

namespace TestMod.Benchmarks;

internal class AttachedClass
{
    public int Value { get; set; } = 1;
}

internal class RefTestClass
{
    public AttachedClass Property { get; set; } = new();
}

[AttachField<RefTestClass, AttachedClass>("Dict", Impl = AttachFieldImpl.StrongRef)]
[AttachField<RefTestClass, AttachedClass>("Custom", Impl = AttachFieldImpl.WeakRef)]
internal static partial class AttachedRefFields
{
    private static ConditionalWeakTable<RefTestClass, AttachedClass> s_CWTFields = new();

    extension(RefTestClass attachedTo)
    {
        public AttachedClass CWT
        {
            get => s_CWTFields.GetValue(attachedTo, (_) => new());
            set
            {
                if (s_CWTFields.TryGetValue(attachedTo, out _))
                {
                    s_CWTFields.Remove(attachedTo);
                }
                s_CWTFields.Add(attachedTo, value);
            }
        }
        public static void ClearAllCWTFields() => s_CWTFields = new();
    }
}

public class ReadRef
{
    private RefTestClass obj = new();

    [Params(1, 10, 100)]
    public int ObjectCount;

    [Params(1_000_000)]
    public int Iterations;

    [IterationSetup]
    public void Setup()
    {
        for (int i = 0; i < ObjectCount - 1; i++)
        {
            RefTestClass o = new();
            o.CWT.Value++;
            o.Custom.Value++;
            o.Dict.Value++;
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        RefTestClass.ClearAllCustomFields();
        RefTestClass.ClearAllCWTFields();
        RefTestClass.ClearAllDictFields();
        GC.Collect();
    }

    [Benchmark]
    public int Custom()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            res += obj.Custom.Value;
        }
        return res;
    }

    [Benchmark]
    public int CWT()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            res += obj.CWT.Value;
        }
        return res;
    }

    [Benchmark]
    public int Dict()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            res += obj.Dict.Value;
        }
        return res;
    }

    [Benchmark]
    public int Property()
    {
        int res = 0;
        for (int i = 0; i < Iterations; i++)
        {
            res += obj.Property.Value;
        }
        return res;
    }
}

public class RandomRefTest
{
    private RefTestClass[] obj = null!;
    public int[] actions = null!;
    public int[] indexes = null!;

    private WeakReference<RefTestClass>[] objWeakRef = null!;

    [Params(200)]
    public int ObjCount;

    [Params(5_000_000)]
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
        obj = new RefTestClass[ObjCount];
        objWeakRef = new WeakReference<RefTestClass>[ObjCount];
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

        RefTestClass.ClearAllCustomFields();
        RefTestClass.ClearAllCWTFields();
        RefTestClass.ClearAllDictFields();
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
                res += obj[indexes[i]].Custom.Value;
            else if (a == 1)
                obj[indexes[i]].Custom.Value++;
            else if (a == 2)
                obj[indexes[i]].Custom.Value--;
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
                    res += obj[indexes[i]].CWT.Value;
                else if (a == 1)
                    obj[indexes[i]].CWT.Value++;
                else if (a == 2)
                    obj[indexes[i]].CWT.Value--;
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
                res += obj[indexes[i]].Dict.Value;
            else if (a == 1)
                obj[indexes[i]].Dict.Value++;
            else if (a == 2)
                obj[indexes[i]].Dict.Value--;
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
                res += obj[indexes[i]].Property.Value;
            else if (a == 1)
                obj[indexes[i]].Property.Value++;
            else if (a == 2)
                obj[indexes[i]].Property.Value--;
            else
                obj[indexes[i]] = new();
        }
        return res;
    }
}
