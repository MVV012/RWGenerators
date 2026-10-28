using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using ImGuiNET;
using LogUtils;
using MonoMod.RuntimeDetour;
using RWCustom;
using RWIMGUI.API;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TestMod;

internal class ImGUIWindow
{
    private static delegate* managed<ref IntPtr, ref uint, ref uint, void> s_customMenuCallback;
    public static void OnEnable()
    {
        unsafe
        {
            s_customMenuCallback = &ShowWindow;
            ImGUIAPI.AddCustomMenuCallback(s_customMenuCallback);
        }
        Plugin.ManualHooks.Add(new Hook(
            typeof(IMGUIContext).GetMethod(nameof(IMGUIContext.BlockWMEvent)),
            BlockWMEvent_Hook
        ));

        Plugin.OnDisableEvent += OnDisable;
    }

    public static void OnDisable()
    {
        unsafe
        {
            ImGUIAPI.RemoveCustomMenuCallback(s_customMenuCallback);
            s_customMenuCallback = null;
        }
    }

    private static bool BlockWMEvent_Hook(Func<IMGUIContext, bool> orig, IMGUIContext self)
    {
        return orig(self) && (ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) || ImGui.IsAnyItemHovered());
    }

    public static void ShowWindow(ref IntPtr IDXGISwapChain, ref uint SyncInterval, ref uint Flags)
    {
        //ImGui.GetIO().MouseDrawCursor = false;
        if (ImGui.Begin("RWGenerators TestMod"))
        {
            WindowContent();
        }
        ImGui.End();
    }

    private static Type[] benchmarkTypes = typeof(ImGUIWindow).Assembly
        .GetTypes()
        .Where(type => type.Namespace == "TestMod.Benchmarks")
        .Where(type => type
            .GetMethods()
            .Any(method => method
                .GetCustomAttributes()
                .Any(attribute => attribute is BenchmarkAttribute)
            )
        )
        .ToArray();

    private static int capacity = 8;
    private static int objects = 10000;
    private static void WindowContent()
    {
        if (ImGui.TreeNodeEx("Hash codes", ImGuiTreeNodeFlags.Framed))
        {
            ImGui.Unindent();

            ImGui.InputInt("Capacity", ref capacity);
            ImGui.InputInt("Objects", ref objects);

            if (ImGui.Button("Test hash codes distribution"))
            {
                var obj = new StrongBox<int>[objects];
                for (int i = 0; i < objects; i++) obj[i] = new();

                int[] count = new int[capacity];
                Random rand = new();

                for (int i = 0; i < objects; i++)
                {
                    int hash = RuntimeHelpers.GetHashCode(obj[i]);
                    int res = (hash >> 8) & (capacity - 1);
                    count[res]++;

                    i += rand.Next(0, 10);
                }
                for (int i = 0; i < capacity; i++)
                {
                    Log.Log($"{i}: {count[i]}");
                }
            }
            ImGui.Indent();
            ImGui.TreePop();
        }

        if (ImGui.TreeNodeEx("Benchmarks", ImGuiTreeNodeFlags.Framed))
        {
            ImGui.Unindent();
            foreach (Type benchmarkType in benchmarkTypes)
            {
                if (ImGui.Button($"{benchmarkType.Name}"))
                {
                    RunBenchmarkDefault(benchmarkType);
                }
            }
            ImGui.Indent();
            ImGui.TreePop();
        }

        if (ImGui.Button("CWT leak test setup"))
        {
            CWTLeakTestSetup();
        }
        if (ImGui.Button("CWT leak test"))
        {
            CWTLeakTest();
        }
    }

    private static void RunBenchmarkDefault(Type type)
    {
        RunOnMainThread(() => BenchmarkRunner.Run(type, DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            //.AddDiagnoser(MemoryDiagnoser.Default)
            .AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance))
        ));
    }
    private static void RunBenchmarkDefault<T>() => RunBenchmarkDefault(typeof(T));

    private class WithFinalizer
    {
        ~WithFinalizer()
        {
            Log.Log("Finalizer called");
            Log.Log($"{GC.CollectionCount(0)} {GC.CollectionCount(1)} {GC.CollectionCount(2)}");
        }
    }

    //private static ConditionalWeakTable<WithFinalizer, StrongBox<int>> cwt = new();
    private static RWGenerators.Collections.WeakTable<WithFinalizer, StrongBox<int>> cwt = new();
    private static WeakReference weakRef = null!;
    private static void CWTLeakTestSetup()
    {
        WithFinalizer? obj = new();
        weakRef = new(obj);
        //cwt.GetOrCreateValue(obj);
        cwt.SetValue(obj, new(10));
        GC.Collect();
    }
    private static void CWTLeakTest()
    {
        GC.Collect();
        Log.Log($"Object is alive: {weakRef.IsAlive}");
        Log.Log($"{GC.CollectionCount(0)} {GC.CollectionCount(1)} {GC.CollectionCount(2)}");
    }

    private static void RunOnMainThread(Action action)
    {
        UtilityCore.Scheduler.Schedule(action, frameInterval: 1, invokeLimit: 1);
    }
}

internal static class RWGameUtils
{
    public static bool TryGetRWGame(out RainWorldGame game)
    {
        if (Custom.rainWorld.processManagerInitialized && Custom.rainWorld.processManager.currentMainLoop is RainWorldGame rwgame)
        {
            game = rwgame;
            return true;
        }
        else
        {
            game = null!;
            return false;
        }
    }
}
