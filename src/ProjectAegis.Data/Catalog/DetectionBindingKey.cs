namespace ProjectAegis.Data.Catalog;

/// <summary>Lookup key for catalog sensor basePd (ordinal string compare).</summary>
public readonly record struct DetectionBindingKey(string PlatformId, string SensorId)
{
    public int CompareTo(DetectionBindingKey other)
    {
        var platform = string.Compare(PlatformId, other.PlatformId, StringComparison.Ordinal);
        if (platform != 0)
        {
            return platform;
        }

        return string.Compare(SensorId, other.SensorId, StringComparison.Ordinal);
    }
}