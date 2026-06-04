namespace ProjectAegis.Delegation.Projection;

public static class UnitDetailPanelBinder
{
    public static UnitDetailPanelState Bind(UnitDetailEntry? entry, string? contactLine = null)
    {
        if (entry == null)
        {
            return new UnitDetailPanelState(
                "UNIT: —",
                "STATUS: —",
                "MAGAZINE: —",
                "EMCON: —",
                "DOCTRINE: —",
                "FUEL: —",
                "ENGAGE: —",
                contactLine ?? "CONTACT: —");
        }

        return new UnitDetailPanelState(
            $"UNIT: {entry.UnitId}",
            $"STATUS: {entry.StatusLabel}",
            entry.MagazineLabel,
            entry.EmconLabel,
            entry.DoctrineLabel,
            entry.FuelLabel,
            entry.EngagePreviewLabel,
            contactLine ?? "CONTACT: —");
    }
}