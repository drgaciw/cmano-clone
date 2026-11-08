namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Sprite-sheet slice metadata for APP-6 USS atlas frames (S26-06 texture pack).
/// Headless hosts validate slice coverage; Unity loads via Addressables key.
/// </summary>
public readonly record struct App6AtlasSpriteSlice(int X, int Y, int Width, int Height)
{
    public float NormalizedX => X / (float)App6AtlasSpriteSheet.SheetWidth;

    public float NormalizedY => Y / (float)App6AtlasSpriteSheet.SheetHeight;

    public float NormalizedWidth => Width / (float)App6AtlasSpriteSheet.SheetWidth;

    public float NormalizedHeight => Height / (float)App6AtlasSpriteSheet.SheetHeight;
}

/// <summary>Addressables + USS texture atlas contract for APP-6 frame sprites.</summary>
public static class App6AtlasSpriteSheet
{
    public const string AddressableGroup = "MapPresentation";

    public const string AddressableKey = "Map/App6FrameAtlas";

    public const string TextureAssetPath = "Assets/UI/MapPlaceholder/App6FrameAtlas.png";

    public const int FrameWidth = 16;

    public const int FrameHeight = 16;

    public const int SheetWidth = FrameWidth * 7;

    public const int SheetHeight = FrameHeight;

    private static readonly IReadOnlyDictionary<string, App6AtlasSpriteSlice> SliceTable =
        new Dictionary<string, App6AtlasSpriteSlice>(StringComparer.Ordinal)
        {
            [App6Sidc.FriendlySurfaceUnitFrame] = Slice(0),
            [App6Sidc.HostileContactFrame] = Slice(1),
            [App6Sidc.FriendlyDestroyedFrame] = Slice(2),
            [App6Sidc.FallbackFrame] = Slice(3),
            [App6Sidc.NeutralUnitFrame] = Slice(4),
            [App6Sidc.SuspectContactFrame] = Slice(5),
            [App6Sidc.PendingUnitFrame] = Slice(6),
        };

    public static IReadOnlyDictionary<string, App6AtlasSpriteSlice> FrameSlices => SliceTable;

    public static bool TryGetSlice(string ussFrameId, out App6AtlasSpriteSlice slice) =>
        SliceTable.TryGetValue(ussFrameId, out slice);

    public static App6AtlasSpriteSlice Slice(int column) =>
        new(column * FrameWidth, 0, FrameWidth, FrameHeight);
}