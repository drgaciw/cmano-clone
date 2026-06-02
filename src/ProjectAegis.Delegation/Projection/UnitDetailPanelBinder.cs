namespace ProjectAegis.Delegation.Projection;

public static class UnitDetailPanelBinder
{
    public static UnitDetailPanelState Bind(UnitDetailEntry? entry)
    {
        if (entry == null)
        {
            return new UnitDetailPanelState(
                "UNIT: —",
                "STATUS: —",
                "MAGAZINE: —",
                "EMCON: —",
                "DOCTRINE: —");
        }

        return new UnitDetailPanelState(
            $"UNIT: {entry.UnitId}",
            $"STATUS: {entry.StatusLabel}",
            entry.MagazineLabel,
            entry.EmconLabel,
            entry.DoctrineLabel);
    }
}