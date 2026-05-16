using BepInEx.Logging;

namespace CustomEverything.App;

internal static class CustomEverythingLog
{
    private static ManualLogSource _logger;

    internal static void Configure(ManualLogSource logger)
    {
        _logger = logger;
    }

    internal static void Info(string message)
    {
        _logger?.LogInfo(message);
    }

    internal static void Warn(string message)
    {
        _logger?.LogWarning(message);
    }

    internal static void Error(string message)
    {
        _logger?.LogError(message);
    }
}
