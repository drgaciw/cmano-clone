using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>Default <see cref="ISensorC2PanelBridge"/> — delegates to headless projection binder.</summary>
public sealed class SensorC2PanelBridge : ISensorC2PanelBridge
{
    public static readonly ISensorC2PanelBridge Default = new SensorC2PanelBridge();

    public SensorC2PanelState BindPanel(SensorC2Snapshot snapshot) =>
        SensorC2PanelBinder.Bind(snapshot);
}
