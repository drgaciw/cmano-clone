namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Catalog;

/// <summary>Maps catalog link latency to effective datalink share lag at harness bind (S34-04).</summary>
public static class DatalinkShareLagResolver
{
    private const double DefaultTickRateHz = 60.0;
    private const string FallbackLinkId = "NATO_TADIL_J";

    public static ScenarioDatalinkDoctrine Resolve(ScenarioDatalinkDoctrine doctrine, ICatalogReader catalog)
    {
        if (doctrine.ShareLagTicksSpecified)
        {
            return doctrine;
        }

        if (!doctrine.IsSharingEnabled)
        {
            return doctrine;
        }

        var primaryLinkId = ResolvePrimaryLinkId(catalog);
        if (!catalog.TryGetLinkLatencyMs(primaryLinkId, out var latencyMs))
        {
            return doctrine;
        }

        var shareLagTicks = (int)Math.Ceiling(latencyMs / (1000.0 / DefaultTickRateHz));
        return doctrine with { ShareLagTicks = shareLagTicks };
    }

    private static string ResolvePrimaryLinkId(ICatalogReader catalog)
    {
        var comms = catalog.GetSortedComms();
        if (comms.Count > 0)
        {
            return comms[0].LinkId;
        }

        var links = catalog.GetSortedLinks();
        if (links.Count > 0)
        {
            return links[0].LinkId;
        }

        return FallbackLinkId;
    }
}