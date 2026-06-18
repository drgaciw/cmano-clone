namespace ProjectAegis.Delegation.Projection;

/// <summary>Default in-memory APP-6 USS frame registry for headless and Unity hosts.</summary>
public sealed class App6AtlasCatalog : IApp6AtlasAvailability
{
    public static App6AtlasCatalog Default { get; } = new(App6Sidc.KnownUssFrameIds, isLoaded: true);

    public static App6AtlasCatalog Unavailable { get; } = new(Array.Empty<string>(), isLoaded: false);

    private readonly HashSet<string> _frameIds;

    public App6AtlasCatalog(IEnumerable<string> frameIds, bool isLoaded)
    {
        _frameIds = new HashSet<string>(frameIds, StringComparer.Ordinal);
        IsLoaded = isLoaded;
    }

    public bool IsLoaded { get; }

    public bool HasFrame(string ussFrameId) =>
        !string.IsNullOrEmpty(ussFrameId) && _frameIds.Contains(ussFrameId);
}