using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestMod.Benchmarks;

public class ArrayIndexRef
{
    [StructLayout(LayoutKind.Auto)]
    private struct Entry<TValue>
    {
        public int hash;
        public int next;
        public GCHandle key;
        public TValue value;
    }

    private Entry<bool>[] entries = null!;

    [Params(1000)]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        entries = new Entry<bool>[Count];
    }

    [Benchmark]
    public int IndexAccess3()
    {
        int res = 0;
        for (int i = 0; i < Count; i++)
        {
            res += (!entries[i].key.IsAllocated &&
                    entries[i].next == 0 &&
                    !entries[i].value) ? 1 : 0;
        }
        return res;
    }

    [Benchmark]
    public int IndexAccess6()
    {
        int res = 0;
        for (int i = 0; i < Count; i++)
        {
            res += (!entries[i].key.IsAllocated &&
                    entries[i].next == 0 &&
                    !entries[i].value &&
                    entries[i].hash == 0 &&
                    entries[i].next != 10000 &&
                    entries[i].key.Equals(default)) ? 1 : 0;
        }
        return res;

    }

    [Benchmark]
    public int RefAccess3()
    {
        int res = 0;
        for (int i = 0; i < Count; i++)
        {
            ref Entry<bool> entry = ref entries[i];

            res += (!entry.key.IsAllocated &&
                    entry.next == 0 &&
                    !entry.value) ? 1 : 0;
        }
        return res;
    }

    [Benchmark]
    public int RefAccess6()
    {
        int res = 0;
        for (int i = 0; i < Count; i++)
        {
            ref Entry<bool> entry = ref entries[i];
            res += (!entry.key.IsAllocated &&
                    entry.next == 0 &&
                    !entry.value &&
                    entry.hash == 0 &&
                    entry.next != 10000 &&
                    entry.key.Equals(default)) ? 1 : 0;
        }
        return res;
    }
}

public class WeakRefs
{
    public object obj = new();
    public object obj2 = new();
    public GCHandle handle;
    public WeakReference weakRef = null!;
    public WeakReference<object> weakRefT = null!;

    [Params(1_000_000)]
    public int Iterations;

    [GlobalSetup]
    public void Setup()
    {
        handle = GCHandle.Alloc(obj, GCHandleType.Weak);
        weakRef = new WeakReference(obj);
        weakRefT = new WeakReference<object>(obj);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        handle.Free();
        weakRef = null!;
        weakRefT = null!;
    }

    [Benchmark]
    public void GCFreeAlloc()
    {
        for (int i = 0; i < Iterations; i++)
        {
            handle.Free();
            handle = GCHandle.Alloc(obj2, GCHandleType.Weak);
        }
    }

    [Benchmark]
    public void GCSetTarget()
    {
        for (int i = 0; i < Iterations; i++)
        {
            handle.Target = obj2;
        }
    }

    [Benchmark]
    public int GCRead()
    {
        int a = 0;
        for (int i = 0; i < Iterations; i++)
        {
            a += (handle.Target != null) ? 1 : 0;
        }
        return a;
    }

    [Benchmark]
    public void WeakNew()
    {
        for (int i = 0; i < Iterations; i++)
        {
            weakRef = null!;
            weakRef = new WeakReference(obj2);
        }
    }

    [Benchmark]
    public void WeakSetTarget()
    {
        for (int i = 0; i < Iterations; i++)
        {
            weakRef.Target = obj2;
        }
    }

    [Benchmark]
    public int WeakRead()
    {
        int a = 0;
        for (int i = 0; i < Iterations; i++)
        {
            a += (weakRef.Target != null) ? 1 : 0;
        }
        return a;
    }

    [Benchmark]
    public void WeakTNew()
    {
        for (int i = 0; i < Iterations; i++)
        {
            weakRefT = null!;
            weakRefT = new WeakReference<object>(obj2);
        }
    }

    [Benchmark]
    public void WeakTSetTarget()
    {
        for (int i = 0; i < Iterations; i++)
        {
            weakRefT.SetTarget(obj2);
        }
    }

    [Benchmark]
    public int WeakTRead()
    {
        int a = 0;
        for (int i = 0; i < Iterations; i++)
        {
            a += (weakRefT.TryGetTarget(out _)) ? 1 : 0;
        }
        return a;
    }
}

public class LambdaInliningTest // They don't get inlined
{
    [Benchmark]
    public int Inlined() => AcceptInline(5);

    [Benchmark]
    public int NotInlined() => AcceptNoInline(5);

    [Benchmark]
    public int LambdaInlined() => AcceptInline(() => 5);

    [Benchmark]
    public int LambdaNotInlined() => AcceptNoInline(() => 5);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AcceptInline(int num) => num;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int AcceptNoInline(int num) => num;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AcceptInline(Func<int> func) => func();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int AcceptNoInline(Func<int> func) => func();
}

public class EE : ExtEnum<EE>
{
    public EE(string name, bool register = false) : base(name, register) { }

    public static readonly EE A = new("A", true);
    public static readonly EE B = new("B", true);
    public static readonly EE C = new("C", true);
    public static readonly EE D = new("D", true);
    public static readonly EE E = new("E", true);
    public static readonly EE F = new("F", true);
    public static readonly EE G = new("G", true);
    public static readonly EE H = new("H", true);
    public static readonly EE Z = new("Z", true);
}
public class ExtEnumSwitch
{

    [Params("A", "D", "H", "Z")]
    public string testedName = null!;
    public EE tested = null!;

    [GlobalSetup]
    public void Setup() => tested = new(testedName);

    [Benchmark]
    public int IfElse4()
    {
        if (tested == EE.A) return 1;
        if (tested == EE.B) return 2;
        if (tested == EE.C) return 3;
        if (tested == EE.D) return 4;
        return -1;
    }

    public static readonly Dictionary<EE, int> Dict4 = new() {
        { EE.A, 1 },
        { EE.B, 2 },
        { EE.C, 3 },
        { EE.D, 4 },
    };
    [Benchmark]
    public int DictGet4()
    {
        return Dict4.TryGetValue(tested, out int value) ? value : -1;
    }

    [Benchmark]
    public int IfElse8()
    {
        if (tested == EE.A) return 1;
        if (tested == EE.B) return 2;
        if (tested == EE.C) return 3;
        if (tested == EE.D) return 4;
        if (tested == EE.E) return 5;
        if (tested == EE.F) return 6;
        if (tested == EE.G) return 7;
        if (tested == EE.H) return 8;
        return -1;
    }

    public static readonly Dictionary<EE, int> Dict8 = new() {
        { EE.A, 1 },
        { EE.B, 2 },
        { EE.C, 3 },
        { EE.D, 4 },
        { EE.E, 5 },
        { EE.F, 6 },
        { EE.G, 7 },
        { EE.H, 8 },
    };
    [Benchmark]
    public int DictGet8()
    {
        return Dict8.TryGetValue(tested, out int value) ? value : -1;
    }
}

public class Misc
{
    public object obj = new();

    [Benchmark]
    public int GCCollectionCount() => GC.CollectionCount(0);

    [Benchmark]
    public int HashMod() => (RuntimeHelpers.GetHashCode(obj) & int.MaxValue) % 17;

    [Benchmark]
    public int HashBit() => (RuntimeHelpers.GetHashCode(obj) >> 8) & (16 - 1);

    [Benchmark]
    public object NewT() => new object();

    [Benchmark]
    public object LambdaNewT() => new Func<object>(() => new object()).Invoke();

    [Benchmark]
    public object ActivatorCreateInstance() => Activator.CreateInstance<object>();
}
