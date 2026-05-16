using System;
using System.Runtime.CompilerServices;
using CustomEverything.App;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;

namespace CustomEverything.Components;

[HarmonyPatch(typeof(SceneInspector), "OnAttachComponentPressed")]
internal static class ComponentPickerPatch
{
    private static readonly ConditionalWeakTable<ComponentSelector, SceneInspector> SelectorOwners = new();

    private static bool Prefix(SceneInspector __instance, IButton button, ButtonEventData eventData)
    {
        var uri = CustomEverythingPlugin.Settings.ComponentPickerUri;
        if (uri == null || __instance.ComponentView.Target == null)
            return true;

        var pickerSlot = __instance.LocalUserSpace.AddSlot("Component Selector");
        pickerSlot.StartTask(async () =>
        {
            try
            {
                await pickerSlot.LoadObjectAsync(uri);
                var inventoryItem = pickerSlot.GetComponent<InventoryItem>();
                var pickerRoot = inventoryItem?.Unpack() ?? pickerSlot;
                var selector = pickerRoot.GetComponentInChildren<ComponentSelector>(ComponentPickerFeature.IsComponentPicker);
                if (selector == null)
                {
                    CustomEverythingLog.Warn("Custom component picker has no compatible ComponentSelector; falling back to stock picker");
                    pickerRoot.Destroy();
                    RunOriginalOnAttachComponentPressed(__instance, button, eventData);
                    return;
                }

                RegisterSelector(__instance, selector);
                PositionLikeStockPicker(__instance, eventData, pickerRoot);
                DestroyWithInspector(__instance, pickerRoot);
            }
            catch (Exception ex)
            {
                CustomEverythingLog.Error($"Failed to load custom component picker: {ex}");
                if (!pickerSlot.IsDestroyed)
                    pickerSlot.Destroy();
                if (!__instance.IsRemoved)
                    RunOriginalOnAttachComponentPressed(__instance, button, eventData);
            }
        });

        return false;
    }

    private static void RegisterSelector(SceneInspector inspector, ComponentSelector selector)
    {
        SelectorOwners.Remove(selector);
        SelectorOwners.Add(selector, inspector);
        selector.ComponentSelected.Target = OnComponentSelected;
    }

    private static void PositionLikeStockPicker(SceneInspector inspector, ButtonEventData eventData, Slot pickerRoot)
    {
        pickerRoot.GlobalPosition = eventData.globalPoint + inspector.Slot.Forward * -0.05f * inspector.LocalUserRoot.GlobalScale;
        pickerRoot.GlobalRotation = inspector.Slot.GlobalRotation;
        pickerRoot.GlobalScale *= inspector.LocalUserRoot.GlobalScale;
    }

    private static void DestroyWithInspector(SceneInspector inspector, Slot pickerRoot)
    {
        var destroyProxy = inspector.DestroyWhenDestroyed(pickerRoot);
        destroyProxy.Persistent = false;
        pickerRoot.DestroyWhenDestroyed(destroyProxy);
    }

    [SyncMethod(typeof(ComponentSelectionHandler), new string[] { })]
    private static void OnComponentSelected(ComponentSelector selector, Type componentType)
    {
        if (!SelectorOwners.TryGetValue(selector, out var inspector) || inspector.IsRemoved)
            return;

        RunOriginalOnComponentSelected(inspector, selector, componentType);
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SceneInspector), "OnAttachComponentPressed")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunOriginalOnAttachComponentPressed(SceneInspector instance, IButton button, ButtonEventData eventData)
    {
        throw new NotSupportedException("Harmony reverse patch stub");
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SceneInspector), "OnComponentSelected")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunOriginalOnComponentSelected(SceneInspector instance, ComponentSelector selector, Type componentType)
    {
        throw new NotSupportedException("Harmony reverse patch stub");
    }
}
