using System.Collections.Generic;
using CustomEverything.App;
using CustomEverything.Inventory;
using FrooxEngine;
using HarmonyLib;

namespace CustomEverything.ProtoFlux;

[HarmonyPatch(typeof(SlotHelper), "GenerateTags", [typeof(Slot), typeof(HashSet<string>)])]
internal static class ProtoFluxBrowserTagPatch
{
    private static void Postfix(Slot slot, HashSet<string> tags)
    {
        var selector = slot.GetComponent<ComponentSelector>();
        if (selector?.ComponentSelected.Target?.Method == ProtoFluxBrowserFeature.NodeTypeSelectedMethod)
            tags.Add(ProtoFluxBrowserFeature.BrowserTag);
    }
}

[HarmonyPatch(typeof(InventoryBrowser))]
internal static class ProtoFluxBrowserInventoryBrowserPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("OnItemSelected")]
    private static void AfterItemSelected(InventoryBrowser __instance, BrowserItem currentItem)
    {
        InventorySelectionButton.AddForTaggedItem(
            __instance,
            currentItem,
            ProtoFluxBrowserFeature.BrowserTag,
            "CustomEverything Set ProtoFlux Browser",
            "Favourite ProtoFlux Browser",
            ToggleProtoFluxBrowserFavorite);
    }

    private static void ToggleProtoFluxBrowserFavorite(InventoryBrowser browser, System.Uri uri)
    {
        if (CustomEverythingPlugin.Settings.IsProtoFluxBrowserUri(uri))
            uri = null;

        CustomEverythingPlugin.Settings.SetProtoFluxBrowserUri(uri);
        InventoryBrowserAccess.ReprocessItems(browser);
        CustomEverythingLog.Info(uri == null ? "Cleared favourite ProtoFlux browser" : $"Favourite ProtoFlux browser set to {uri}");
    }
}
