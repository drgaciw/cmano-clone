namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>
/// CMD-11 / CMD-26: headless tests for GetEngagePreviewForUnit live feed
/// and GroundOps thin OOB-derived feed logic (no Unity MonoBehaviour).
/// </summary>
[TestFixture]
public sealed class LiveFeedProjectionTests
{
    // ──────────────────────────────────────────────────────────────
    // CMD-11: GetEngagePreviewForUnit
    // ──────────────────────────────────────────────────────────────

    [Test]
    public void GetEngagePreviewForUnit_returns_null_for_unknown_unit()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var snapshot = new StubSnapshot(SimTime: 1, ContactCount: 2, hasFireControlTrack: true);

        var preview = bridge.GetEngagePreviewForUnit("no-such-unit", snapshot);

        Assert.That(preview, Is.Null);
    }

    [Test]
    public void GetEngagePreviewForUnit_returns_preview_for_registered_unit()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.BeginExecution();

        var snapshot = new StubSnapshot(SimTime: 1, ContactCount: 2, hasFireControlTrack: true);
        var preview = bridge.GetEngagePreviewForUnit("u1", snapshot);

        Assert.That(preview, Is.Not.Null);
        Assert.That(preview!.DlzLabel, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GetEngagePreviewForUnit_blocked_when_no_fire_control_track()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.BeginExecution();

        var snapshot = new StubSnapshot(SimTime: 1, ContactCount: 2, hasFireControlTrack: false);
        var preview = bridge.GetEngagePreviewForUnit("u1", snapshot);

        Assert.That(preview, Is.Not.Null);
        Assert.That(preview!.CanFire, Is.False);
        Assert.That(preview.AbortPreviewCode, Does.Contain("NO_FIRE_CONTROL").IgnoreCase);
    }

    [Test]
    public void EngageExplainProjection_wraps_GetEngagePreviewForUnit_result()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.BeginExecution();

        var snapshot = new StubSnapshot(SimTime: 1, ContactCount: 2, hasFireControlTrack: true);
        var preview = bridge.GetEngagePreviewForUnit("u1", snapshot);
        var explain = EngageExplainProjection.Project(preview);

        Assert.That(explain, Is.Not.Null);
        Assert.That(explain.StatusLine, Is.Not.Null.And.Not.Empty);
        Assert.That(explain.ReasonPlain, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void EngageExplainProjection_returns_empty_for_null_preview()
    {
        // Mirrors ProjectSelectedEngageExplain() fallback: no selection → null preview → Empty.
        var explain = EngageExplainProjection.Project(null);

        Assert.That(explain, Is.EqualTo(EngageExplain.Empty));
        Assert.That(explain.IsBlocked, Is.False);
        Assert.That(explain.StatusLine, Is.EqualTo(EngageExplainProjection.NoPreviewLabel));
    }

    // ──────────────────────────────────────────────────────────────
    // CMD-26: thin OOB ground feed logic (headless via projection)
    // ──────────────────────────────────────────────────────────────

    [Test]
    public void OobTreeEntry_alive_maps_to_strength_green_formation()
    {
        var oob = new[] { new OobTreeEntry("u1", IsAlive: true) };
        var inputs = BuildGroundInputsFromOob(oob);
        var entries = GroundOpsProjection.Project(inputs);

        Assert.That(entries.Count, Is.EqualTo(1));
        Assert.That(entries[0].FormationId, Is.EqualTo("u1"));
        Assert.That(entries[0].DisplayName, Is.EqualTo("u1"));
        Assert.That(entries[0].SideLabel, Is.EqualTo("BLUE"));
        Assert.That(entries[0].EchelonLabel, Is.EqualTo("Brigade+"));
        Assert.That(entries[0].StrengthBand, Is.EqualTo("STRENGTH: GREEN"));
    }

    [Test]
    public void OobTreeEntry_dead_maps_to_strength_dash_formation()
    {
        var oob = new[] { new OobTreeEntry("u2", IsAlive: false) };
        var inputs = BuildGroundInputsFromOob(oob);
        var entries = GroundOpsProjection.Project(inputs);

        Assert.That(entries.Count, Is.EqualTo(1));
        Assert.That(entries[0].StrengthBand, Is.EqualTo("STRENGTH: —"));
    }

    [Test]
    public void Multiple_oob_entries_produce_ordered_ground_entries()
    {
        var oob = new[]
        {
            new OobTreeEntry("u3", IsAlive: true),
            new OobTreeEntry("u1", IsAlive: false),
            new OobTreeEntry("u2", IsAlive: true),
        };
        var inputs = BuildGroundInputsFromOob(oob);
        var entries = GroundOpsProjection.Project(inputs);

        Assert.That(entries.Count, Is.EqualTo(3));
        // GroundOpsProjection orders by FormationId (ordinal)
        Assert.That(entries[0].FormationId, Is.EqualTo("u1"));
        Assert.That(entries[1].FormationId, Is.EqualTo("u2"));
        Assert.That(entries[2].FormationId, Is.EqualTo("u3"));
    }

    /// <summary>
    /// Replicates the thin-feed mapping in GroundOpsPanelHost.Refresh()
    /// so it can be tested headlessly without Unity MonoBehaviour.
    /// </summary>
    private static IReadOnlyList<GroundFormationInput> BuildGroundInputsFromOob(
        IReadOnlyList<OobTreeEntry> oob)
    {
        var inputs = new System.Collections.Generic.List<GroundFormationInput>(oob.Count);
        foreach (var entry in oob)
        {
            inputs.Add(new GroundFormationInput(
                FormationId: entry.UnitId,
                DisplayName: entry.UnitId,
                SideLabel: "BLUE",
                EchelonLabel: "Brigade+",
                LocationLabel: null,
                StrengthFraction: entry.IsAlive ? 1.0 : 0.0,
                AdaAssetCount: 0,
                AdaOnlineCount: 0,
                FacilityKind: null,
                FacilityDamageFraction: 0.0));
        }

        return inputs;
    }

    // ──────────────────────────────────────────────────────────────
    // Shared test doubles
    // ──────────────────────────────────────────────────────────────

    private sealed class StubSnapshot(
        double SimTime,
        int ContactCount,
        bool hasFireControlTrack = true) : ISimWorldSnapshot
    {
        public double SimTime { get; } = SimTime;
        public int ContactCount { get; } = ContactCount;
        public int ActiveEngagementCount { get; } = 0;
        public TargetId? PrimaryHostileContactId =>
            ContactCount > 0 ? new TargetId("hostile-1") : null;
        public bool HasFireControlTrackOnPrimaryContact => hasFireControlTrack;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => true;
    }
}
