using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Thin presentation adapter for Cesium / placeholder map hosts (ADR-018 production path, ADR-010).
/// Implementations live in Unity Runtime hosts; headless tests use <see cref="HeadlessGlobeMapSurface"/>.
/// Never mutates sim state — camera jump + symbol readback only.
/// </summary>
public interface IGlobeMapSurface
{
    /// <summary>
    /// When false (CI default), hosts keep the MapPlaceholder path. Must remain false on
    /// DelegationSmoke / headless CI per ADR-018.
    /// </summary>
    bool UseGlobeMap { get; }

    /// <summary>Active theater bounds used for NDC ↔ WGS84 mapping and camera framing.</summary>
    GeographicBounds ActiveTheaterBounds { get; }

    /// <summary>Active theater id (e.g. <see cref="TheaterQuickJump.BalticId"/>).</summary>
    string ActiveTheaterId { get; }

    /// <summary>Read-only map symbols currently shown (NDC space relative to active theater).</summary>
    IReadOnlyList<MapSymbolEntry> CurrentSymbols { get; }

    /// <summary>
    /// Jump the view/camera to a named theater. Presentation-only; no sim mutation.
    /// </summary>
    void JumpToTheater(TheaterDefinition theater);

    /// <summary>
    /// Convenience: resolve <paramref name="theaterIdOrAlias"/> via <see cref="TheaterQuickJump"/>
    /// and jump. Returns false when the id is unknown (active theater unchanged).
    /// </summary>
    bool TryJumpToTheater(string? theaterIdOrAlias);
}

/// <summary>
/// Headless <see cref="IGlobeMapSurface"/> for unit tests and CI — records jump targets without
/// UnityEngine or Cesium package references.
/// </summary>
public sealed class HeadlessGlobeMapSurface : IGlobeMapSurface
{
    private readonly List<MapSymbolEntry> _symbols = new();

    public HeadlessGlobeMapSurface(
        bool useGlobeMap = false,
        TheaterDefinition? initialTheater = null,
        IEnumerable<MapSymbolEntry>? symbols = null)
    {
        UseGlobeMap = useGlobeMap;
        var theater = initialTheater ?? TheaterQuickJump.Baltic;
        ActiveTheaterId = theater.Id;
        ActiveTheaterBounds = theater.Bounds;
        LastJumpedTheater = theater;
        if (symbols != null)
        {
            _symbols.AddRange(symbols);
        }
    }

    /// <inheritdoc />
    public bool UseGlobeMap { get; }

    /// <inheritdoc />
    public GeographicBounds ActiveTheaterBounds { get; private set; }

    /// <inheritdoc />
    public string ActiveTheaterId { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<MapSymbolEntry> CurrentSymbols => _symbols;

    /// <summary>Last theater passed to <see cref="JumpToTheater"/> (starts as constructor theater).</summary>
    public TheaterDefinition LastJumpedTheater { get; private set; }

    /// <summary>Number of successful jumps (including constructor baseline as 0).</summary>
    public int JumpCount { get; private set; }

    /// <summary>Replace the symbol list (tests / host refresh). Presentation-only.</summary>
    public void SetSymbols(IEnumerable<MapSymbolEntry> symbols)
    {
        _symbols.Clear();
        if (symbols != null)
        {
            _symbols.AddRange(symbols);
        }
    }

    /// <inheritdoc />
    public void JumpToTheater(TheaterDefinition theater)
    {
        if (theater is null)
        {
            throw new ArgumentNullException(nameof(theater));
        }

        ActiveTheaterId = theater.Id;
        ActiveTheaterBounds = theater.Bounds;
        LastJumpedTheater = theater;
        JumpCount++;
    }

    /// <inheritdoc />
    public bool TryJumpToTheater(string? theaterIdOrAlias)
    {
        var theater = TheaterQuickJump.Resolve(theaterIdOrAlias);
        if (theater is null)
        {
            return false;
        }

        JumpToTheater(theater);
        return true;
    }
}
