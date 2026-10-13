using BepInEx;
using LogUtils;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using System.Security.Permissions;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace TestMod;

[BepInAutoPlugin]
sealed public partial class Plugin : BaseUnityPlugin
{
    private bool _isInit = false;
    internal static bool DebugWindowEnabled = false;

    internal static List<Hook> ManualHooks = [];
    internal static event Action? OnDisableEvent;

    public void OnEnable()
    {
        LogWrapper.OnEnable(Logger);
        Log.LogDebug("OnEnable");

        ImGUIWindow.OnEnable();

        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
    }

    public void OnDisable()
    {
        HookEndpointManager.RemoveAllOwnedBy(typeof(Plugin).Assembly);
        foreach (Hook hook in ManualHooks)
        {
            hook.Dispose();
        }
        ManualHooks.Clear();

        OnDisableEvent?.Invoke();
        OnDisableEvent = null;

        LogWrapper.OnDisable();
    }

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        if (_isInit) return;
        _isInit = true;
    }
}
