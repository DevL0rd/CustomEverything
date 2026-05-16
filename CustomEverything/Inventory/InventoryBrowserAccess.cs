using FrooxEngine;
using FrooxEngine.Store;
using HarmonyLib;
using System;
using System.Reflection;

namespace CustomEverything.Inventory;

internal static class InventoryBrowserAccess
{
    private static readonly AccessTools.FieldRef<InventoryItemUI, Record> ItemField =
        AccessTools.FieldRefAccess<InventoryItemUI, Record>("Item");

    private static readonly AccessTools.FieldRef<InventoryBrowser, SyncRef<Slot>> ButtonsRootField =
        AccessTools.FieldRefAccess<InventoryBrowser, SyncRef<Slot>>("_buttonsRoot");

    private static readonly MethodInfo ReprocessItemsMethod =
        AccessTools.Method(typeof(InventoryBrowser), "ReprocessItems");

    internal static Record GetRecord(InventoryItemUI item)
    {
        return item == null ? null : ItemField(item);
    }

    internal static Record GetSelectedRecord(InventoryBrowser browser)
    {
        return GetRecord(browser.SelectedInventoryItem);
    }

    internal static Slot GetButtonsRoot(InventoryBrowser browser)
    {
        return ButtonsRootField(browser).Target;
    }

    internal static void ReprocessItems(InventoryBrowser browser)
    {
        ReprocessItemsMethod.Invoke(browser, Array.Empty<object>());
    }
}
