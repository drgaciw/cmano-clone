using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

/// <summary>Minimal <see cref="IReadOnlyOrderLog"/> for fixture-backed projection tests.</summary>
internal sealed class StubReadOnlyOrderLog : IReadOnlyOrderLog
{
    private readonly IReadOnlyList<OrderLogEntry> _entries;
    private readonly string _fingerprint;

    public StubReadOnlyOrderLog(IReadOnlyList<OrderLogEntry> entries, string fingerprint)
    {
        _entries = entries;
        _fingerprint = fingerprint;
    }

    public IReadOnlyList<OrderLogEntry> ChronologicalEntries() => _entries;

    public string ComputeFingerprint() => _fingerprint;

    public IReadOnlyList<DecisionRecord> Records { get; init; } = Array.Empty<DecisionRecord>();

    public IReadOnlyList<PolicyDenialRecord> PolicyDenials { get; init; } =
        Array.Empty<PolicyDenialRecord>();

    public IReadOnlyList<EngagementRecord> Engagements { get; init; } =
        Array.Empty<EngagementRecord>();

    public IReadOnlyList<ControllerChangeRecord> ControllerChanges { get; init; } =
        Array.Empty<ControllerChangeRecord>();
}
