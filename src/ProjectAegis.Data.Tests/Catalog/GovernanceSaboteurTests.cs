using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// AEGIS-309 / DRG-239: Tests proving that governance regressions (mutants 18-21)
/// are detected and rejected by both governance unit tests and gauntlet oracle evaluators.
/// 
/// Mutants calibrated:
/// - 18-autonomy-semi-bypass: AutonomyGate SemiAutonomous approval gating
/// - 19-approval-autopause-suppressed: WatchAutoPauseGate ShouldAutoPausePendingApprovals
/// - 20-cumulative-salvo-bypassed: PolicyEvaluator cumulative salvo ledger limit
/// - 21-wra-range-conflation-restored: MvpEngagementResolver WraRange to WraRangeDenial (not OutOfEnvelope)
/// </summary>
public sealed class GovernanceSaboteurTests
{
    // =========================================================================
    // Mutant 18: Autonomy Semi-Bypass (AutonomyGate)
    // =========================================================================

    [Fact]
    public void Mutant18_AutonomyGate_semi_autonomous_unauthorized_lethal_fire_order_must_queue_for_approval()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var lethalOrder = new Order(new OrderId(1), new TargetId("target-1"), 0, OrderKind.Engage, RiskLevel.High);

        var result = gate.Evaluate(AutonomyLevel.SemiAutonomous, lethalOrder, playerApproved: false);

        // Mutant 18 bypasses this gate and returns ExecuteNow = true, QueueForApproval = false.
        // The governance contract requires lethal unapproved orders under SemiAutonomous to be queued for approval.
        Assert.False(result.ExecuteNow, "SemiAutonomous lethal fire order without player approval must NOT execute immediately.");
        Assert.True(result.QueueForApproval, "SemiAutonomous lethal fire order without player approval MUST be queued for approval.");
        Assert.False(result.Rejected);
    }

    [Fact]
    public void Mutant18_GauntletOracle_detects_unauthorized_lethal_execution_via_missiles_fired_expect()
    {
        // When SemiAutonomous safety gate is bypassed, automated fire produces unauthorized missile launches.
        var policy = """
            {
              "id": "gauntlet-gov-autonomy",
              "gauntlet": {
                "expect": {
                  "side": "BLUE",
                  "maxMissilesFired": 0,
                  "requireNonEmptyFingerprint": true
                }
              }
            }
            """;
        var csv = """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            gauntlet-gov-autonomy,42,BLUE,100,1,2,0,Engagement|1|1|1|u1|1|True|Launched
            """;

        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.False(result.Passed, "Oracle must fail closed when unauthorized missiles are fired due to autonomy bypass.");
        Assert.Contains(result.Failures, f => f.Contains("missilesFired", StringComparison.Ordinal));
    }

    // =========================================================================
    // Mutant 19: Approval Auto-Pause Suppressed (WatchAutoPauseGate)
    // =========================================================================

    [Fact]
    public void Mutant19_WatchAutoPauseGate_should_auto_pause_when_pending_approval_queue_has_proposals()
    {
        var gate = new WatchAutoPauseGate();
        var queue = new PendingApprovalQueue { AutoPauseGate = gate };
        gate.PendingApprovalQueue = queue;

        var order = new Order(new OrderId(42), new TargetId("t42"), 1.0, OrderKind.Engage, RiskLevel.High);
        queue.Enqueue(order);

        // Mutant 19 mutates ShouldAutoPausePendingApprovals() to return false.
        // The governance contract requires that pending approval proposals trigger auto-pause.
        var shouldPause = gate.ShouldAutoPausePendingApprovals();
        Assert.True(shouldPause, "ShouldAutoPausePendingApprovals must return true when pending approval proposals exist.");
        Assert.Equal(WatchPauseReason.OrderProposal, gate.LastPauseReason);
    }

    [Fact]
    public void Mutant19_GauntletOracle_detects_suppressed_autopause_via_required_pause_token()
    {
        // Gauntlet runs require that order proposal pause tokens appear in decision / fingerprint logs.
        var policy = """
            {
              "id": "gauntlet-gov-autopause",
              "gauntlet": {
                "expect": {
                  "requireNonEmptyFingerprint": true,
                  "requireFingerprintSubstrings": [ "PauseSim|OrderProposal" ]
                }
              }
            }
            """;
        var csvWithSuppressedPause = """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            gauntlet-gov-autopause,42,BLUE,50,0,0,0,Tick|1|Tick|2|OrderExecuted|42
            """;

        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csvWithSuppressedPause);
        Assert.False(result.Passed, "Oracle must fail closed when PauseSim|OrderProposal token is missing from fingerprint.");
        Assert.Contains(result.Failures, f => f.Contains("PauseSim|OrderProposal", StringComparison.Ordinal));
    }

    // =========================================================================
    // Mutant 20: Cumulative Salvo Bypassed (PolicyEvaluator)
    // =========================================================================

    [Fact]
    public void Mutant20_PolicyEvaluator_enforces_cumulative_salvo_ledger_limit()
    {
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2);
        var evaluator = new PolicyEvaluator(_ => policy);
        const ulong targetTrackId = 77;
        const ulong tick = 50;

        // Register 2 rounds already fired at target track 77 within the active window
        evaluator.RegisterFired(targetTrackId, count: 2, simTick: tick, windowTicks: 100);

        var ctx = new PolicyContext(UnitId: 1, PolicySnapshotId: 0, SimTick: tick, Effective: policy, SalvoSize: 1);
        var request = new ActionRequest(ActionKind.FireGuided, targetTrackId, MountId: 1);

        // Mutant 20 bypasses cumulative salvo ledger check, returning PolicyVerdict.Allow().
        // The governance contract requires that cumulative + salvo > MaxSalvo is denied with FireAbortReason.WraSalvo.
        var verdict = evaluator.Evaluate(ctx, request);
        Assert.False(verdict.Allowed, "Evaluating fire when cumulative salvo exceeds MaxSalvo must be denied.");
        Assert.Equal(FireAbortReason.WraSalvo, verdict.Reason);
    }

    [Fact]
    public void Mutant20_GauntletOracle_detects_cumulative_salvo_breach_via_max_missiles()
    {
        var policy = """
            {
              "id": "gauntlet-gov-salvo",
              "gauntlet": {
                "expect": {
                  "side": "BLUE",
                  "maxMissilesFired": 2,
                  "requireNonEmptyFingerprint": true
                }
              }
            }
            """;
        var csv = """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            gauntlet-gov-salvo,42,BLUE,200,2,4,0,PolicyUpdate|1|Engagement|2|Engagement|3|Engagement|4
            """;

        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.False(result.Passed, "Oracle must fail closed when cumulative salvo limit breach produces excessive missiles.");
        Assert.Contains(result.Failures, f => f.Contains("missilesFired 4 > max 2", StringComparison.Ordinal));
    }

    // =========================================================================
    // Mutant 21: WRA Range Conflation Restored (MvpEngagementResolver)
    // =========================================================================

    [Fact]
    public void Mutant21_MvpEngagementResolver_maps_WraRange_to_WraRangeDenial_not_OutOfEnvelope()
    {
        var world = new SingleContextWorldQuery(new EngageContext(
            RangeMeters: 50_000,
            Envelope: new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 2,
            HasFireControlTrack: true));
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);

        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            new DenyReasonPolicyEvaluator(FireAbortReason.WraRange));

        var request = new EngageRequest(ShooterUnitId: 1, TargetId: 2, MountId: 0, SimTick: 10);
        var result = resolver.Resolve(request);

        // Mutant 21 maps FireAbortReason.WraRange back to EngagementAbortReason.OutOfEnvelope.
        // The governance contract (AEGIS-304 / DRG-234) requires WraRangeDenial to distinguish doctrine range gate from kinematic envelope.
        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.WraRangeDenial, result.AbortReason);
        Assert.NotEqual(EngagementAbortReason.OutOfEnvelope, result.AbortReason);
    }

    [Fact]
    public void Mutant21_GauntletOracle_detects_conflation_via_required_fingerprint_substring()
    {
        var policy = """
            {
              "id": "gauntlet-gov-wra-range",
              "gauntlet": {
                "expect": {
                  "requireNonEmptyFingerprint": true,
                  "requireFingerprintSubstrings": [ "WraRangeDenial" ]
                }
              }
            }
            """;
        // When conflated back to OutOfEnvelope, WraRangeDenial does not appear in fingerprint
        var csv = """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            gauntlet-gov-wra-range,42,BLUE,0,0,0,1,EngagementAbort|10|1|2|OutOfEnvelope
            """;

        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.False(result.Passed, "Oracle must fail closed when WraRangeDenial is missing due to OutOfEnvelope conflation.");
        Assert.Contains(result.Failures, f => f.Contains("WraRangeDenial", StringComparison.Ordinal));
    }

    // =========================================================================
    // Helper types for engagement test isolation
    // =========================================================================

    private sealed class DenyReasonPolicyEvaluator : IPolicyEvaluator
    {
        private readonly FireAbortReason _reason;

        public DenyReasonPolicyEvaluator(FireAbortReason reason) => _reason = reason;

        public PolicyVerdict Evaluate(in PolicyContext ctx, in ActionRequest request) =>
            PolicyVerdict.Deny(_reason);
    }

    private sealed class SingleContextWorldQuery : IEngageWorldQuery
    {
        private readonly EngageContext _context;

        public SingleContextWorldQuery(EngageContext context) => _context = context;

        public bool TryGetContext(in EngageRequest request, out EngageContext context)
        {
            context = _context;
            return true;
        }
    }
}
