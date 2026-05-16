using System.Threading;
using BepInEx;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using BepisResoniteWrapper;
using CustomEverything.Configuration;
using HarmonyLib;

namespace CustomEverything.App;

[ResonitePlugin(PluginGuid, PluginName, PluginVersion, PluginAuthor, PluginUrl)]
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
public sealed class CustomEverythingPlugin : BasePlugin
{
    internal const string PluginGuid = "com.devl0rd.CustomEverything";
    internal const string PluginName = "CustomEverything";
    internal const string PluginAuthor = "DevL0rd";
    internal const string PluginVersion = CustomEverythingVersionInfo.Version;
    internal const string PluginUrl = "https://github.com/DevL0rd/CustomEverything";

    private static int _initialized;

    internal static CustomEverythingConfig Settings { get; private set; }

    public override void Load()
    {
        CustomEverythingLog.Configure(Log);
        Settings = new CustomEverythingConfig(base.Config);
        ResoniteHooks.OnEngineReady += OnEngineReady;
        CustomEverythingLog.Info("Plugin loaded; waiting for Resonite engine");
    }

    private static void OnEngineReady()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
            return;

        var harmony = new Harmony(PluginGuid);
        harmony.PatchAll(typeof(CustomEverythingPlugin).Assembly);
        CustomEverythingLog.Info("CustomEverything initialized");
    }
}
