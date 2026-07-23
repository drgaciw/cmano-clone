namespace ProjectAegis.Data.WriteGate;

public interface ICatalogClock
{
    long UtcTicks { get; }
}

public sealed class FixedCatalogClock : ICatalogClock
{
    public FixedCatalogClock(long utcTicks) => UtcTicks = utcTicks;

    public long UtcTicks { get; }
}

/// <summary>Monotonic clock for deterministic unique write-gate batch ids per propose call.</summary>
public sealed class IncrementingCatalogClock : ICatalogClock
{
    private long _next;

    public IncrementingCatalogClock(long start = 0) => _next = start;

    public long UtcTicks => _next++;
}