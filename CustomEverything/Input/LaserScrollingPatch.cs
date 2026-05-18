using CustomEverything.App;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace CustomEverything.Input;

[HarmonyPatch(typeof(InteractionHandler), "OnInputUpdate")]
internal static class LaserScrollingPatch
{
    private const float LaserScrollSpeed = 120f;

    private static void Postfix(InteractionHandler __instance)
    {
        if (!CustomEverythingPlugin.Settings.EnableLaserScrolling ||
            __instance.InputInterface.ScreenActive ||
            !__instance.Inputs.Axis.RegisterBlocks)
            return;

        if (__instance.Laser.CurrentTouchable is not IAxisActionReceiver receiver)
            return;

        var axis = __instance.Inputs.Axis.Value.Value * new float2(-1f, 1f);
        receiver.ProcessAxis(__instance.Laser.TouchSource, axis * LaserScrollSpeed);
    }
}
