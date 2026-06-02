namespace ProjectAegis.Delegation.Projection;

/// <summary>Maps <see cref="SensorC2Snapshot"/> to UI Toolkit–friendly labels (GDD sensor C2 left drawer).</summary>
public static class SensorC2PanelBinder
{
    public static SensorC2PanelState Bind(SensorC2Snapshot snapshot)
    {
        var rows = new List<SensorC2ContactRow>(snapshot.Contacts.Count);
        foreach (var contact in snapshot.Contacts)
        {
            rows.Add(new SensorC2ContactRow(
                contact.ContactId,
                contact.LifecycleState,
                contact.TargetId,
                FormatContactLine(contact.ContactId, contact.LifecycleState, contact.TargetId)));
        }

        return new SensorC2PanelState(
            snapshot.ObserverRadarEmconActive ? "EMCON: ACTIVE" : "EMCON: OFF",
            snapshot.HasFireControlTrackOnPrimary ? "TRACK: FC" : "TRACK: —",
            $"CONTACTS: {snapshot.ActiveContactCount}",
            rows);
    }

    private static string FormatContactLine(string contactId, string state, string targetId) =>
        $"{contactId}  {state}  → {targetId}";
}