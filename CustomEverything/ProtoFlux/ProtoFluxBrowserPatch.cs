using System;
using CustomEverything.App;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;

namespace CustomEverything.ProtoFlux;

[HarmonyPatch(typeof(FrooxEngine.ProtoFlux.ProtoFluxTool), "OpenNodeBrowser")]
internal static class ProtoFluxBrowserPatch
{
    private static bool Prefix(FrooxEngine.ProtoFlux.ProtoFluxTool __instance)
    {
        var settings = CustomEverythingPlugin.Settings;
        var browserUri = settings.ProtoFluxBrowserUri;
        if (browserUri == null)
            return true;

        var slot = __instance.LocalUserSpace.AddSlot("NodeMenu");
        slot.StartTask(async () =>
        {
            try
            {
                await slot.LoadObjectAsync(browserUri);
                var inventoryItem = slot.GetComponent<InventoryItem>();
                var browserRoot = inventoryItem?.Unpack() ?? slot;

                browserRoot.PositionInFrontOfUser(float3.Backward);
                browserRoot.GlobalScale = browserRoot.World.LocalUser.Root.Slot.GlobalScale * settings.ProtoFluxBrowserScale;
            }
            catch (Exception ex)
            {
                CustomEverythingLog.Error($"Failed to load custom ProtoFlux browser: {ex}");
                if (!slot.IsDestroyed)
                    slot.Destroy();
            }
        });

        __instance.ActiveHandler?.CloseContextMenu();
        return false;
    }
}
