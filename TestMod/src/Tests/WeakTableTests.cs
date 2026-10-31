using LogUtils.Diagnostics;
using LogUtils.Diagnostics.Tests;
using RWGenerators.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TestMod.Tests;

public class WeakTableTests : TestCaseGroup, ITestable
{
    public WeakTableTests() : base("WeakTableTests") { }

    public void Test()
    {
        try
        {
            AddCullDead(1);
            AddCullDead(100);
            AddCullDead(1000);

            AddThenRemoveAll(1);
            AddThenRemoveAll(100);
            AddThenRemoveAll(1000);

            RemoveAddIteratively(100, 1);
            RemoveAddIteratively(1000, 25);
            RemoveAddIteratively(1000, 500);

            ClearAllValuesRemoved(0, 5);
            ClearAllValuesRemoved(1, 5);
            ClearAllValuesRemoved(100, 5);
        }
        catch (Exception e)
        {
            this.Fail($"Exception: {e}");
        }
    }

    [PostTest]
    public void ShowResults()
    {
        TestRunner.TestLogger.Log(CreateReport());
    }

    // Most tests are based on https://github.com/dotnet/runtime/blob/main/src/libraries/System.Runtime/tests/System.Runtime.Tests/System/Runtime/CompilerServices/ConditionalWeakTableTests.cs
    public void AddCullDead(int numObjects)
    {
        using TestCaseGroup addCullDeadGroup = new(this, $"AddCullDead({numObjects})");

        Func<int, (WeakTable<object, object>, WeakReference[], WeakReference[])> body = count =>
        {
            using TestCaseGroup group = new(addCullDeadGroup, $"AddCullDead({numObjects}) - Adding");

            object[] keys = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
            object[] values = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
            var wt = new WeakTable<object, object>();

            for (int i = 0; i < numObjects; i++)
            {
                wt.SetValue(keys[i], values[i]);
            }

            for (int i = 0; i < numObjects; i++)
            {
                object value;

                group.AssertThat(wt.TryGetValue(keys[i], out value)).IsTrue();
                group.AssertThat(value).IsEqualTo(values[i]);
                group.AssertThat(value).IsEqualTo(wt.GetValue(keys[i], () => new()));

                wt.CullDead();
                group.AssertThat(wt.TryGetValue(keys[i], out value)).IsTrue();
                group.AssertThat(value).IsEqualTo(values[i]);
            }

            for (int i = 0; i < numObjects; i++)
            {
                group.AssertThat(wt.TryGetValue(keys[i], out object value)).IsTrue();
                group.AssertThat(value).IsEqualTo(values[i]);
            }

            return (wt, keys.Select(k => new WeakReference(k)).ToArray(), values.Select(v => new WeakReference(v)).ToArray());
        };

        (WeakTable<object, object> wt, WeakReference[] keys, WeakReference[] values) = body(numObjects);

        using TestCaseGroup group = new(addCullDeadGroup, $"AddCullDead({numObjects}) - Culled dead");

        int gcTimeout = 500;

        GC.Collect();
        group.AssertThat(WaitForGC(keys[0], gcTimeout)).IsTrue().OnFail($"Keys were not cleared in {gcTimeout}ms");

        for (int i = 0; i < numObjects; i++)
        {
            group.AssertThat(keys[i].IsAlive).IsFalse().OnFail($"Expected not to find key #{i}");
            group.AssertThat(values[i].IsAlive).IsTrue().OnFail($"Value #{i} somehow died");
        }

        wt.CullDead();
        GC.Collect();
        group.AssertThat(WaitForGC(values[0], gcTimeout)).IsTrue().OnFail($"Values were not cleared in {gcTimeout}ms");

        for (int i = 0; i < numObjects; i++)
        {
            group.AssertThat(keys[i].IsAlive).IsFalse().OnFail($"Expected not to find key #{i}");
            group.AssertThat(values[i].IsAlive).IsFalse().OnFail($"Expected not to find value #{i}");
        }
    }

    public bool WaitForGC(WeakReference reference, int timeout)
    {
        Stopwatch sw = new();
        sw.Start();
        while (reference.IsAlive)
        {
            if (sw.ElapsedMilliseconds >= timeout)
            {
                return false;
            }
            Thread.Sleep(10);
        }
        sw.Stop();
        return true;
    }

    public void AddThenRemoveAll(int numObjects)
    {
        using TestCaseGroup group = new(this, $"AddThenRemoveAll({numObjects})");

        object[] keys = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
        object[] values = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
        var wt = new WeakTable<object, object>();

        for (int i = 0; i < numObjects; i++)
        {
            wt.SetValue(keys[i], values[i]);
        }

        for (int i = 0; i < numObjects; i++)
        {
            group.AssertThat(wt.GetValue(keys[i], () => new())).IsEqualTo(values[i]);
        }

        for (int i = 0; i < numObjects; i++)
        {
            wt.Remove(keys[i]);
            group.AssertThat(wt.TryGetValue(keys[i], out _)).IsFalse();
        }

        for (int i = 0; i < numObjects; i++)
        {
            group.AssertThat(wt.TryGetValue(keys[i], out _)).IsFalse();
        }
    }

    public void RemoveAddIteratively(int numObjects, int removeBy)
    {
        using TestCaseGroup group = new(this, $"RemoveAddIteratively({numObjects}, {removeBy})");

        object[] keys = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
        object[] values = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
        var wt = new WeakTable<object, object>();

        for (int i = 0; i < numObjects; i++)
        {
            wt.SetValue(keys[i], values[i]);
            group.AssertThat(wt.GetValue(keys[i], () => new())).IsEqualTo(values[i]);
        }

        for (int i = 0; i < numObjects; i += removeBy)
        {
            for (int j = i; j < Math.Min(i + removeBy, numObjects); j++)
            {
                wt.Remove(keys[j]);
                group.AssertThat(wt.TryGetValue(keys[j], out _)).IsFalse();
            }
            for (int j = i; j < Math.Min(i + removeBy, numObjects); j++)
            {
                wt.SetValue(keys[j], values[j]);
                group.AssertThat(wt.GetValue(keys[j], () => new())).IsEqualTo(values[j]);
            }
        }

        for (int i = 0; i < numObjects; i++)
        {
            group.AssertThat(wt.GetValue(keys[i], () => new())).IsEqualTo(values[i]);
        }
    }

    public void ClearAllValuesRemoved(int numObjects, int iters)
    {
        using TestCaseGroup group = new(this, $"ClearAllValuesRemoved({numObjects}, {iters})");

        var wt = new WeakTable<object, object>();

        object[] keys = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
        object[] values = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();

        for (int iter = 0; iter < iters; iter++)
        {
            for (int i = 0; i < numObjects; i++)
            {
                wt.SetValue(keys[i], values[i]);
                group.AssertThat(wt.GetValue(keys[i], () => new())).IsEqualTo(values[i]);
            }

            wt.Clear();

            for (int i = 0; i < numObjects; i++)
            {
                group.AssertThat(wt.TryGetValue(keys[i], out _)).IsFalse();
            }
        }
    }
}
