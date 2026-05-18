using System;
using BepInEx.Configuration;

namespace CustomEverything.Configuration;

internal sealed class CustomEverythingConfig
{
    private readonly ConfigFile _config;
    private readonly ConfigEntry<string> _inspectorUri;
    private readonly ConfigEntry<string> _protoFluxBrowserUri;
    private readonly ConfigEntry<string> _componentPickerUri;
    private readonly ConfigEntry<bool> _enableLaserScrolling;

    internal CustomEverythingConfig(ConfigFile config)
    {
        _config = config;
        _inspectorUri = config.Bind(
            "Inspectors",
            "InspectorUri",
            string.Empty,
            "Selected custom inspector inventory URI.");
        _protoFluxBrowserUri = config.Bind(
            "ProtoFlux",
            "BrowserUri",
            string.Empty,
            "Selected custom ProtoFlux browser inventory URI.");
        _componentPickerUri = config.Bind(
            "Components",
            "PickerUri",
            string.Empty,
            "Selected custom component picker inventory URI.");
        _enableLaserScrolling = config.Bind(
            "Input",
            "EnableLaserScrolling",
            true,
            "Scroll laser-targeted UI with controller stick or touchpad input.");
    }

    internal float InspectorScale => 0.0005f;

    internal Uri InspectorUri
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_inspectorUri.Value))
                return null;
            return Uri.TryCreate(_inspectorUri.Value, UriKind.Absolute, out var uri) ? uri : null;
        }
    }

    internal bool IsInspectorUri(Uri uri)
    {
        return uri != null && InspectorUri == uri;
    }

    internal void SetInspectorUri(Uri uri)
    {
        _inspectorUri.Value = uri?.ToString() ?? string.Empty;
        _config.Save();
    }

    internal float ProtoFluxBrowserScale => 1f;

    internal Uri ProtoFluxBrowserUri
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_protoFluxBrowserUri.Value))
                return null;
            return Uri.TryCreate(_protoFluxBrowserUri.Value, UriKind.Absolute, out var uri) ? uri : null;
        }
    }

    internal bool IsProtoFluxBrowserUri(Uri uri)
    {
        return uri != null && ProtoFluxBrowserUri == uri;
    }

    internal void SetProtoFluxBrowserUri(Uri uri)
    {
        _protoFluxBrowserUri.Value = uri?.ToString() ?? string.Empty;
        _config.Save();
    }

    internal Uri ComponentPickerUri
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_componentPickerUri.Value))
                return null;
            return Uri.TryCreate(_componentPickerUri.Value, UriKind.Absolute, out var uri) ? uri : null;
        }
    }

    internal bool IsComponentPickerUri(Uri uri)
    {
        return uri != null && ComponentPickerUri == uri;
    }

    internal void SetComponentPickerUri(Uri uri)
    {
        _componentPickerUri.Value = uri?.ToString() ?? string.Empty;
        _config.Save();
    }

    internal bool EnableLaserScrolling => _enableLaserScrolling.Value;
}
