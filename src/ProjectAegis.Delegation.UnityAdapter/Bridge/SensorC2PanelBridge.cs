using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>
/// Default <see cref="ISensorC2PanelBridge"/> — delegates to headless projection binder.
/// Presentation-only adapter (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed class SensorC2PanelBridge : ISensorC2PanelBridge
{
    public static readonly ISensorC2PanelBridge Default = new SensorC2PanelBridge();

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">When snapshot is null.</exception>
    public SensorC2PanelState BindPanel(SensorC2Snapshot snapshot)
    {
        // netstandard2.1 (Unity plugins): no ArgumentNullException.ThrowIfNull
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return SensorC2PanelBinder.Bind(snapshot);
    }
}
