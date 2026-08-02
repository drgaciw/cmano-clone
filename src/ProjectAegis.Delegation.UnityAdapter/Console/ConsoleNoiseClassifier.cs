namespace ProjectAegis.Delegation.UnityAdapter.Console;

/// <summary>
/// Classifies Unity Editor/Player console lines into project-owned vs package-tool vs environment noise.
/// Pure string fingerprinting — used by Play Mode sign-off and offline integrity tests.
/// </summary>
public static class ConsoleNoiseClassifier
{
    public enum NoiseClass
    {
        ProjectOwned,
        PackageTool,
        Environment,
        Unknown,
    }

    /// <summary>
    /// Returns true when a log line should not fail Play Mode / console gates
    /// (package-tool spam or environment-only residuals).
    /// </summary>
    public static bool IsIgnorableForConsoleGate(string? condition, string? stackTrace = null)
    {
        var kind = Classify(condition, stackTrace);
        return kind is NoiseClass.PackageTool or NoiseClass.Environment;
    }

    public static NoiseClass Classify(string? condition, string? stackTrace = null)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return NoiseClass.Unknown;
        }

        // Package / editor-tool spam (MCP cloud hub, AI Game Developer reconnect).
        if (condition.Contains("McpManagerClientHub", StringComparison.Ordinal)
            || condition.Contains("Connection not available and auto-reconnect disabled", StringComparison.Ordinal)
            || condition.Contains("/hub/mcp-server", StringComparison.Ordinal)
            || condition.Contains("Version handshake failed", StringComparison.Ordinal)
            || condition.Contains("Server rejected authorization", StringComparison.Ordinal)
            || condition.Contains("PerformVersionHandshake", StringComparison.Ordinal)
            || condition.Contains("Start Indexing on Editor startup", StringComparison.Ordinal)
            || condition.Contains("Mesh Deformation Systems disabled", StringComparison.Ordinal)
            || condition.Contains("No SRP present", StringComparison.Ordinal)
            || condition.Contains("[AI]", StringComparison.Ordinal) && condition.Contains("McpManager", StringComparison.Ordinal))
        {
            return NoiseClass.PackageTool;
        }

        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            if (stackTrace.Contains("com.IvanMurzak.Unity.MCP", StringComparison.Ordinal)
                || stackTrace.Contains("com.IvanMurzak.McpPlugin", StringComparison.Ordinal)
                || stackTrace.Contains("UnityEditor.Search", StringComparison.Ordinal)
                || stackTrace.Contains("Unity.AI.Toolkit.Accounts", StringComparison.Ordinal))
            {
                return NoiseClass.PackageTool;
            }
        }

        // Environment-only (license client, cloud account API when offline/unfocused).
        if (condition.Contains("Licensing::Client", StringComparison.Ordinal)
            || condition.Contains("Found 0 entitlement groups", StringComparison.Ordinal)
            || condition.Contains("Account API did not become accessible", StringComparison.Ordinal)
            || condition.Contains("ApiAccessibleState", StringComparison.Ordinal))
        {
            return NoiseClass.Environment;
        }

        if (!string.IsNullOrWhiteSpace(stackTrace)
            && stackTrace.Contains("Unity.AI.Toolkit.Accounts.Services.States.ApiAccessibleState", StringComparison.Ordinal))
        {
            return NoiseClass.Environment;
        }

        // Project-owned fingerprints we still treat as real when present.
        if (condition.Contains("Font not found for path: Fonts/RobotoMono", StringComparison.Ordinal)
            || condition.Contains("Font 'RobotoMono-Regular'", StringComparison.Ordinal)
            || condition.Contains("AudioListener", StringComparison.Ordinal)
            || condition.Contains("PanelSettings", StringComparison.Ordinal) && condition.Contains("null", StringComparison.OrdinalIgnoreCase))
        {
            return NoiseClass.ProjectOwned;
        }

        return NoiseClass.Unknown;
    }
}
