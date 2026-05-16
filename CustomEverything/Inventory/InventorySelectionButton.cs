using System;
using FrooxEngine;
using FrooxEngine.UIX;

namespace CustomEverything.Inventory;

internal static class InventorySelectionButton
{
    internal static void AddForTaggedItem(
        InventoryBrowser browser,
        BrowserItem currentItem,
        string requiredTag,
        string slotName,
        string label,
        Action<InventoryBrowser, Uri> onPressed)
    {
        var buttonsRoot = InventoryBrowserAccess.GetButtonsRoot(browser);
        if (buttonsRoot == null)
            return;

        var buttonHost = buttonsRoot[0];
        buttonHost.FindChild(slotName)?.Destroy();

        if (currentItem is not InventoryItemUI itemUi)
            return;

        var record = InventoryBrowserAccess.GetRecord(itemUi);
        if (record?.Tags == null || !record.Tags.Contains(requiredTag))
            return;

        var uri = record.GetUrl(browser.Cloud.Platform);
        if (uri == null)
            return;

        var builder = new UIBuilder(buttonHost);
        builder.Style.PreferredWidth = BrowserDialog.DEFAULT_ITEM_SIZE * 3;
        RadiantUI_Constants.SetupDefaultStyle(builder);

        var button = builder.Button(OfficialAssets.Graphics.Icons.Inspector.Pin, label);
        button.Slot.Name = slotName;
        button.Slot.OrderOffset = -1;
        button.LocalPressed += (_, _) =>
        {
            onPressed(browser, uri);
        };
    }
}
