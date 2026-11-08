#if UNITY_EDITOR
namespace ProjectAegis.Unity.Editor
{
    /// <summary>
    /// Intentionally non-destructive: does not auto-strip AudioListeners or rewrite
    /// MCP UserSettings on load/play.
    /// Explicit AudioListener cleanup remains on
    /// <see cref="DelegationSmokeSceneBuilder"/> (scene build, menu, batch).
    /// MCP quiet mode is a manual UserSettings edit — not applied from editor load hooks.
    /// </summary>
    internal static class ProjectConsoleQuietBootstrap
    {
        // No editor-load hooks: previous auto-mutation paths were adversarial high findings.
    }
}
#endif
