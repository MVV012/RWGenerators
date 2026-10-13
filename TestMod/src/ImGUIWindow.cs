using ImGuiNET;
using LogUtils;
using MonoMod.RuntimeDetour;
using RWIMGUI.API;
using RWCustom;

namespace TestMod;

internal class ImGUIWindow
{
    private unsafe static delegate* managed<ref IntPtr, ref uint, ref uint, void> s_customMenuCallback;
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
        ImGui.GetIO().MouseDrawCursor = false;
        if (ImGui.Begin("RWGenerators TestMod"))
        {
            WindowContent();
        }
        ImGui.End();
    }

    private static void WindowContent()
    {
        //if (!RWGameUtils.TryGetRWGame(out RainWorldGame game))
        //{
        //    ImGui.Text("Game is not opened!");
        //    return;
        //}
        ImGui.Text("Test");
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
