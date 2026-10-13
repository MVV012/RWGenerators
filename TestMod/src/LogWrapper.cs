global using static TestMod.LogWrapper.Global;

using LogUtils;
using LogUtils.Enums;

namespace TestMod;

internal static class LogWrapper
{
    public static class Global
    {
        public static Logger Log = null!;
        public static LogID ID = null!;
    }

    public static BepInEx.Logging.ManualLogSource BepInExLogger = null!;

    public static void OnEnable(BepInEx.Logging.ManualLogSource bepInExLogger)
    {
        BepInExLogger = bepInExLogger;

        ID = new LogID("RWGeneratorsTestMod.log", LogAccess.FullAccess, register: true);
        ID.Properties.ReadOnly = false;
        ID.Properties.ShowCategories.Enable();
        ID.Properties.ConsoleIDs.Add(ConsoleID.BepInEx);
        UpdateProperties(ID);
        ID.Properties.ReadOnly = true;

        Log = new Logger(ID);

        BepInExLogger.LogInfo($"Logging into {ID.Properties.CurrentFilePath}");
    }

    public static void OnDisable()
    {
        ID.Unregister();
    }

    public static void UpdateProperties(LogID logID)
    {
        logID.Properties.ReadOnly = false;
        logID.Properties.IntroMessage = null;
        logID.Properties.OutroMessage = null;
        logID.Properties.LogsFolderEligible = true;
        logID.Properties.ReadOnly = true;
    }
}
