namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>Test double for <see cref="ISimWorldSnapshot"/> contact/sensor fields.</summary>
internal sealed class SimWorldSnapshotStub : ISimWorldSnapshot
{
    public SimWorldSnapshotStub(
        double simTime = 0,
        int contactCount = 0,
        int activeEngagementCount = 0,
        bool memberAlive = true,
        TargetId? primaryHostileContactId = null,
        bool hasFireControlTrackOnPrimaryContact = false)
    {
        SimTime = simTime;
        ContactCount = contactCount;
        ActiveEngagementCount = activeEngagementCount;
        MemberAlive = memberAlive;
        PrimaryHostileContactId = contactCount > 0
            ? primaryHostileContactId ?? new TargetId("hostile-1")
            : null;
        HasFireControlTrackOnPrimaryContact =
            ContactCount > 0 && hasFireControlTrackOnPrimaryContact;
    }

    public double SimTime { get; }

    public int ContactCount { get; }

    public int ActiveEngagementCount { get; }

    public TargetId? PrimaryHostileContactId { get; }

    public bool HasFireControlTrackOnPrimaryContact { get; }

    private bool MemberAlive { get; }

    public bool IsMemberAlive(TargetId memberId) => MemberAlive;
}