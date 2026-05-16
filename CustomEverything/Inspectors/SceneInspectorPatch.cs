using System;
using System.Runtime.CompilerServices;
using CustomEverything.App;
using FrooxEngine;
using HarmonyLib;

namespace CustomEverything.Inspectors;

[HarmonyPatch(typeof(SceneInspector), "OnAttach")]
internal static class SceneInspectorPatch
{
    private static bool Prefix(SceneInspector __instance)
    {
        var uri = CustomEverythingPlugin.Settings.InspectorUri;
        if (uri == null)
            return true;

        var scale = __instance.World.LocalUser.Root.Slot.GlobalScale * CustomEverythingPlugin.Settings.InspectorScale;
        __instance.Slot.LocalScale *= scale;
        __instance.StartTask(async () =>
        {
            await new Updates(0);

            var loaded = await CustomInspectorLoader.TryLoad(__instance, uri);
            if (loaded)
                return;

            CustomEverythingLog.Warn("Custom inspector load failed; falling back to the stock inspector");
            await default(ToWorld);
            if (__instance.IsRemoved || __instance.Slot.IsRemoved)
                return;

            __instance.Slot.LocalScale /= scale;
            RunOriginalOnAttach(__instance);
        });

        return false;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SceneInspector), "OnAttach")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunOriginalOnAttach(SceneInspector instance)
    {
        throw new NotSupportedException("Harmony reverse patch stub");
    }
}
