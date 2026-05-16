using System.Reflection;
using FrooxEngine;
using HarmonyLib;

namespace CustomEverything.Components;

internal static class ComponentPickerFeature
{
    internal const string PickerTag = "custom_everything_component_picker";

    private static readonly MethodInfo ProtoFluxNodeSelectedMethod =
        AccessTools.Method(typeof(FrooxEngine.ProtoFlux.ProtoFluxTool), "OnNodeTypeSelected");

    internal static bool IsComponentPicker(ComponentSelector selector)
    {
        return selector != null &&
               selector.ComponentSelected.Target?.Method != ProtoFluxNodeSelectedMethod;
    }
}
