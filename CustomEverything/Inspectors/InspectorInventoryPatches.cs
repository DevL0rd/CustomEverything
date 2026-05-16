using System.Collections.Generic;
using CustomEverything.App;
using CustomEverything.Inventory;
using FrooxEngine;
using HarmonyLib;

namespace CustomEverything.Inspectors;

[HarmonyPatch(typeof(SlotHelper), "GenerateTags", [typeof(Slot), typeof(HashSet<string>)])]
internal static class InspectorTagPatch
{
    private static void Postfix(Slot slot, HashSet<string> tags)
    {
        if (slot.GetComponent<SceneInspector>() != null)
            tags.Add(InspectorFeature.InspectorTag);
    }
}

[HarmonyPatch(typeof(InventoryBrowser))]
internal static class InspectorInventoryBrowserPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("OnItemSelected")]
    private static void AfterItemSelected(InventoryBrowser __instance, BrowserItem currentItem)
    {
        InventorySelectionButton.AddForTaggedItem(
            __instance,
            currentItem,
            InspectorFeature.InspectorTag,
            "CustomEverything Set Inspector",
            "Favourite Inspector",
            ToggleInspectorFavorite);
    }

    private static void ToggleInspectorFavorite(InventoryBrowser browser, System.Uri uri)
    {
        if (CustomEverythingPlugin.Settings.IsInspectorUri(uri))
            uri = null;

        CustomEverythingPlugin.Settings.SetInspectorUri(uri);
        InventoryBrowserAccess.ReprocessItems(browser);
        CustomEverythingLog.Info(uri == null ? "Cleared favourite inspector" : $"Favourite inspector set to {uri}");
    }
}
