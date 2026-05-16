using System.Collections.Generic;
using CustomEverything.App;
using CustomEverything.Inventory;
using FrooxEngine;
using HarmonyLib;

namespace CustomEverything.Components;

[HarmonyPatch(typeof(SlotHelper), "GenerateTags", [typeof(Slot), typeof(HashSet<string>)])]
internal static class ComponentPickerTagPatch
{
    private static void Postfix(Slot slot, HashSet<string> tags)
    {
        var selector = slot.GetComponent<ComponentSelector>();
        if (ComponentPickerFeature.IsComponentPicker(selector))
            tags.Add(ComponentPickerFeature.PickerTag);
    }
}

[HarmonyPatch(typeof(InventoryBrowser))]
internal static class ComponentPickerInventoryBrowserPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("OnItemSelected")]
    private static void AfterItemSelected(InventoryBrowser __instance, BrowserItem currentItem)
    {
        InventorySelectionButton.AddForTaggedItem(
            __instance,
            currentItem,
            ComponentPickerFeature.PickerTag,
            "CustomEverything Set Component Picker",
            "Favourite Component Picker",
            ToggleComponentPickerFavorite);
    }

    private static void ToggleComponentPickerFavorite(InventoryBrowser browser, System.Uri uri)
    {
        if (CustomEverythingPlugin.Settings.IsComponentPickerUri(uri))
            uri = null;

        CustomEverythingPlugin.Settings.SetComponentPickerUri(uri);
        InventoryBrowserAccess.ReprocessItems(browser);
        CustomEverythingLog.Info(uri == null ? "Cleared favourite component picker" : $"Favourite component picker set to {uri}");
    }
}
