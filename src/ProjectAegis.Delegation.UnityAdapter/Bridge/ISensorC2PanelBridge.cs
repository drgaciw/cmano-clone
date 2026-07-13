using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>
/// Adapter seam for Sensor C2 HUD panel binding (Spirit1 G1 / ADR-010).
/// Unity panel hosts call this instead of <see cref="SensorC2PanelBinder"/> directly
/// so GitNexus can trace host → adapter → projection edges.
/// </summary>
public interface ISensorC2PanelBridge
{
    SensorC2PanelState BindPanel(SensorC2Snapshot snapshot);
}
