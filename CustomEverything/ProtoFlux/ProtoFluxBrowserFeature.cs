using HarmonyLib;

namespace CustomEverything.ProtoFlux;

internal static class ProtoFluxBrowserFeature
{
    internal const string BrowserTag = "custom_everything_protoflux_browser";
    internal static readonly System.Reflection.MethodInfo NodeTypeSelectedMethod =
        AccessTools.Method(typeof(FrooxEngine.ProtoFlux.ProtoFluxTool), "OnNodeTypeSelected");
}
