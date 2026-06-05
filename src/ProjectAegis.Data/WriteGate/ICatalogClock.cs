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