using System;
using CustomEverything.App;
using FrooxEngine;
using HarmonyLib;

namespace CustomEverything.Inventory;

[HarmonyPatch(typeof(InventoryBrowser), "ProcessItem")]
internal static class InventoryFavoriteVisuals
{
    private static void Postfix(InventoryBrowser __instance, InventoryItemUI item)
    {
        var uri = InventoryBrowserAccess.GetRecord(item)?.GetUrl(__instance.Cloud.Platform);
        if (uri == null || !IsCustomFavorite(uri))
            return;

        item.NormalColor.Value = InventoryBrowser.FAVORITE_COLOR;
        item.SelectedColor.Value = InventoryBrowser.FAVORITE_COLOR.MulRGB(2f);
    }

    internal static bool IsCustomFavorite(Uri uri)
    {
        var settings = CustomEverythingPlugin.Settings;
        return settings.IsInspectorUri(uri) ||
               settings.IsProtoFluxBrowserUri(uri) ||
               settings.IsComponentPickerUri(uri);
    }
}
