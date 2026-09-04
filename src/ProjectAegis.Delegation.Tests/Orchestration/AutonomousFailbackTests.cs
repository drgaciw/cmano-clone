namespace ProjectAegis.Delegation.Tests.Orchestration;

using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Sim.Policy;

/// <summary>
/// Verifies C2 node partitioning autonomous failback doctrine per Draft 25 (C2N-02 / AEGIS-307 / DRG-237).
/// Invariants:
/// - When partitioned, units apply configured failback policy (e.g. HoldFire blocks offensive orders).
/// - Healthy and Degraded states allow normal command execution.
/// - Local point-defense and emergency survivability / self-defense are preserved during failback.
/// - Clean null safety.
/// </summary>
[TestFixture]
public sealed class AutonomousFailbackTests
{
    private sealed class CustomRoeFilter : IRoeFilter
    {
        private readonly RoeEvaluation _evaluation;

        public CustomRoeFilter(RoeEvaluation evaluation) => _evaluation = evaluation;

        public RoeEvaluation Evaluate(Order order) => _evaluation;
    }

    private static Order CreateEngageOrder(long id = 1, RiskLevel risk = RiskLevel.High) =>
        new(new OrderId(id), new TargetId("t-hostile"), 10.0, OrderKind.Engage, risk);

    private static Order CreateMoveOrder(long id = 2) =>
        new(new OrderId(id), new TargetId("waypoint-alpha"), 10.0, OrderKind.Move, RiskLevel.Low);

    private static Order CreateRtbOrder(long id = 3) =>
        new(new OrderId(id), new TargetId("base-station"), 10.0, OrderKind.ReturnToBase, RiskLevel.Low);

    private static C2NetworkHealthSnapshot CreateSnapshot(C2NetworkHealthLevel healthLevel) =>
        new(
            healthLevel,
            healthLevel == C2NetworkHealthLevel.Partitioned ? CommsState.Denied : CommsState.Nominal,
            "c2-node-1",
            Array.Empty<C2NetworkLinkHealthEntry>(),
            Array.Empty<C2NetworkContributor>(),
            Array.Empty<C2NetworkLostPath>());

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Healthy & Degraded States Allow Normal Command Execution
    // ─────────────────────────────────────────────────────────────────────────

    [TestCase(C2NetworkHealthLevel.Healthy)]
    [TestCase(C2NetworkHealthLevel.Degraded)]
    public void Healthy_and_degraded_states_allow_offensive_lethal_strike(C2NetworkHealthLevel health)
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.HoldFire);
        var order = CreateEngageOrder();

        var result = policy.Evaluate(health, order);

        Assert.That(result.Allowed, Is.True, "Offensive lethal strikes must execute normally when network is healthy or degraded.");
        Assert.That(result.Denied, Is.False);
        Assert.That(result.DenialReason, Is.EqualTo(FireAbortReason.None));
    }

    [TestCase(C2NetworkHealthLevel.Healthy)]
    [TestCase(C2NetworkHealthLevel.Degraded)]
    public void Healthy_and_degraded_states_allow_nonlethal_orders(C2NetworkHealthLevel health)
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.HoldFire);
        var moveOrder = CreateMoveOrder();

        var result = policy.Evaluate(health, moveOrder);

        Assert.That(result.Allowed, Is.True);
        Assert.That(result.DenialReason, Is.EqualTo(FireAbortReason.None));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Partitioned State: AutonomousFailbackMode.HoldFire
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Partitioned_HoldFire_blocks_offensive_engage_order()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.HoldFire);
        var engageOrder = CreateEngageOrder();

        var result = policy.Evaluate(C2NetworkHealthLevel.Partitioned, engageOrder);

        Assert.That(result.Allowed, Is.False, "Partitioned unit under HoldFire must block offensive strikes.");
        Assert.That(result.Denied, Is.True);
        Assert.That(result.DenialReason, Is.EqualTo(FireAbortReason.RoeHoldFire));
        Assert.That(result.Mode, Is.EqualTo(AutonomousFailbackMode.HoldFire));
    }

    [Test]
    public void Partitioned_HoldFire_blocks_high_risk_offensive_order()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.HoldFire);
        var highRiskOrder = new Order(new OrderId(4), new TargetId("t-radar"), 10.0, OrderKind.SetEwPosture, RiskLevel.High);

        var result = policy.Evaluate(C2NetworkHealthLevel.Partitioned, highRiskOrder);

        Assert.That(result.Allowed, Is.False, "High-risk lethal actions must be blocked under HoldFire when partitioned.");
        Assert.That(result.DenialReason, Is.EqualTo(FireAbortReason.RoeHoldFire));
    }

    [Test]
    public void Partitioned_HoldFire_allows_nonlethal_move_and_sensors_orders()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.HoldFire);
        var moveOrder = CreateMoveOrder();
        var sensorOrder = new Order(new OrderId(5), new TargetId("t-local"), 10.0, OrderKind.SetSensors, RiskLevel.Low);

        var moveResult = policy.Evaluate(C2NetworkHealthLevel.Partitioned, moveOrder);
        var sensorResult = policy.Evaluate(C2NetworkHealthLevel.Partitioned, sensorOrder);

        Assert.That(moveResult.Allowed, Is.True, "Non-lethal maneuver is permitted during HoldFire failback.");
        Assert.That(sensorResult.Allowed, Is.True, "Sensor adjustment is permitted during HoldFire failback.");
    }

    [Test]
    public void Partitioned_HoldFire_preserves_local_point_defense()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.HoldFire);
        var pointDefenseOrder = CreateEngageOrder();

        // Local point-defense against incoming anti-ship missile / torpedo
        var result = policy.Evaluate(
            C2NetworkHealthLevel.Partitioned,
            pointDefenseOrder,
            isPointDefense: true);

        Assert.That(result.Allowed, Is.True, "Local point-defense must NEVER be blocked during C2 partition.");
        Assert.That(result.DenialReason, Is.EqualTo(FireAbortReason.None));
    }

    [Test]
    public void Partitioned_HoldFire_preserves_emergency_self_defense_and_survivability()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.HoldFire);
        var emergencyOrder = CreateEngageOrder();

        var selfDefenseResult = policy.Evaluate(
            C2NetworkHealthLevel.Partitioned,
            emergencyOrder,
            isEmergencySelfDefense: true);

        var survivabilityResult = policy.Evaluate(
            C2NetworkHealthLevel.Partitioned,
            emergencyOrder,
            isEmergencySurvivability: true);

        Assert.That(selfDefenseResult.Allowed, Is.True, "Emergency self-defense must be permitted during failback.");
        Assert.That(survivabilityResult.Allowed, Is.True, "Emergency survivability must be permitted during failback.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Partitioned State: AutonomousFailbackMode.ReturnToBase (RTB)
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Partitioned_ReturnToBase_blocks_offensive_strikes()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.ReturnToBase);
        var engageOrder = CreateEngageOrder();

        var result = policy.Evaluate(C2NetworkHealthLevel.Partitioned, engageOrder);

        Assert.That(result.Allowed, Is.False, "Offensive lethal strikes must be blocked under ReturnToBase mode.");
        Assert.That(result.DenialReason, Is.EqualTo(FireAbortReason.CommsDenied));
        Assert.That(result.Mode, Is.EqualTo(AutonomousFailbackMode.ReturnToBase));
    }

    [Test]
    public void Partitioned_ReturnToBase_allows_rtb_maneuver()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.ReturnToBase);
        var rtbOrder = CreateRtbOrder();

        var result = policy.Evaluate(C2NetworkHealthLevel.Partitioned, rtbOrder);

        Assert.That(result.Allowed, Is.True, "ReturnToBase order must be permitted under RTB failback mode.");
    }

    [Test]
    public void Partitioned_ReturnToBase_preserves_point_defense_and_self_defense()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.ReturnToBase);
        var engageOrder = CreateEngageOrder();

        var pdResult = policy.Evaluate(
            C2NetworkHealthLevel.Partitioned,
            engageOrder,
            isPointDefense: true);

        var sdResult = policy.Evaluate(
            C2NetworkHealthLevel.Partitioned,
            engageOrder,
            isEmergencySelfDefense: true);

        Assert.That(pdResult.Allowed, Is.True, "Point defense must be preserved while returning to base.");
        Assert.That(sdResult.Allowed, Is.True, "Self-defense must be preserved while returning to base.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Partitioned State: AutonomousFailbackMode.ExecuteLocalPackageRoe
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Partitioned_ExecuteLocalPackageRoe_allows_cleared_local_strike()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.ExecuteLocalPackageRoe);
        var engageOrder = CreateEngageOrder();

        var result = policy.Evaluate(
            C2NetworkHealthLevel.Partitioned,
            engageOrder,
            localPackageRoePermits: true);

        Assert.That(result.Allowed, Is.True, "Locally cleared strike must be permitted under ExecuteLocalPackageRoe.");
        Assert.That(result.DenialReason, Is.EqualTo(FireAbortReason.None));
    }

    [Test]
    public void Partitioned_ExecuteLocalPackageRoe_blocks_unauthorized_offensive_strike()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.ExecuteLocalPackageRoe);
        var engageOrder = CreateEngageOrder();

        var result = policy.Evaluate(
            C2NetworkHealthLevel.Partitioned,
            engageOrder,
            localPackageRoePermits: false);

        Assert.That(result.Allowed, Is.False, "Offensive strike outside local ROE clearance must be blocked.");
        Assert.That(result.DenialReason, Is.EqualTo(FireAbortReason.RoeHoldFire));
    }

    [Test]
    public void Partitioned_ExecuteLocalPackageRoe_delegates_to_IRoeFilter()
    {
        var allowRoe = new CustomRoeFilter(RoeEvaluation.Allow());
        var rejectRoe = new CustomRoeFilter(RoeEvaluation.Reject(FireAbortReason.WeaponsTight));

        var policyAllow = new AutonomousFailbackPolicy(AutonomousFailbackMode.ExecuteLocalPackageRoe, allowRoe);
        var policyReject = new AutonomousFailbackPolicy(AutonomousFailbackMode.ExecuteLocalPackageRoe, rejectRoe);

        var order = CreateEngageOrder();

        var allowResult = policyAllow.Evaluate(C2NetworkHealthLevel.Partitioned, order);
        var rejectResult = policyReject.Evaluate(C2NetworkHealthLevel.Partitioned, order);

        Assert.That(allowResult.Allowed, Is.True);
        Assert.That(rejectResult.Allowed, Is.False);
        Assert.That(rejectResult.DenialReason, Is.EqualTo(FireAbortReason.WeaponsTight));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. C2NetworkHealthSnapshot Integration
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Snapshot_integration_evaluates_network_health_accurately()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.HoldFire);
        var order = CreateEngageOrder();

        var healthySnap = CreateSnapshot(C2NetworkHealthLevel.Healthy);
        var degradedSnap = CreateSnapshot(C2NetworkHealthLevel.Degraded);
        var partitionedSnap = CreateSnapshot(C2NetworkHealthLevel.Partitioned);

        Assert.That(policy.Evaluate(healthySnap, order).Allowed, Is.True);
        Assert.That(policy.Evaluate(degradedSnap, order).Allowed, Is.True);
        Assert.That(policy.Evaluate(partitionedSnap, order).Allowed, Is.False);

        // Point-defense preserved on partitioned snapshot
        Assert.That(policy.Evaluate(partitionedSnap, order, isPointDefense: true).Allowed, Is.True);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Context & OrderKind Overloads & Order Factory Helpers
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Context_evaluation_evaluates_verdicts_correctly()
    {
        var contextBlocked = new AutonomousFailbackContext(
            C2NetworkHealthLevel.Partitioned,
            Order: CreateEngageOrder(),
            Mode: AutonomousFailbackMode.HoldFire);

        var contextPermitted = new AutonomousFailbackContext(
            C2NetworkHealthLevel.Partitioned,
            Order: CreateEngageOrder(),
            Mode: AutonomousFailbackMode.HoldFire,
            IsPointDefense: true);

        var resultBlocked = AutonomousFailbackPolicy.EvaluateContext(contextBlocked);
        var resultPermitted = AutonomousFailbackPolicy.EvaluateContext(contextPermitted);

        Assert.That(resultBlocked.Allowed, Is.False);
        Assert.That(resultPermitted.Allowed, Is.True);
    }

    [Test]
    public void OrderKind_overload_evaluates_correctly()
    {
        var policy = new AutonomousFailbackPolicy(AutonomousFailbackMode.HoldFire);

        var lethalResult = policy.Evaluate(C2NetworkHealthLevel.Partitioned, OrderKind.Engage, RiskLevel.High);
        var maneuverResult = policy.Evaluate(C2NetworkHealthLevel.Partitioned, OrderKind.Move, RiskLevel.Low);
        var pdResult = policy.Evaluate(C2NetworkHealthLevel.Partitioned, OrderKind.Engage, RiskLevel.High, isPointDefense: true);

        Assert.That(lethalResult.Allowed, Is.False);
        Assert.That(maneuverResult.Allowed, Is.True);
        Assert.That(pdResult.Allowed, Is.True);
    }

    [Test]
    public void Fallback_order_factory_creates_expected_orders()
    {
        Assert.That(AutonomousFailbackPolicy.GetFailbackOrderKind(AutonomousFailbackMode.ReturnToBase), Is.EqualTo(OrderKind.ReturnToBase));
        Assert.That(AutonomousFailbackPolicy.GetFailbackOrderKind(AutonomousFailbackMode.HoldFire), Is.EqualTo(OrderKind.Hold));

        var order = AutonomousFailbackPolicy.CreateFailbackOrder(
            new OrderId(99),
            new TargetId("unit-1"),
            simTime: 42.0,
            AutonomousFailbackMode.ReturnToBase);

        Assert.That(order.Kind, Is.EqualTo(OrderKind.ReturnToBase));
        Assert.That(order.Id.Value, Is.EqualTo(99));
        Assert.That(order.Target.Value, Is.EqualTo("unit-1"));
        Assert.That(order.Risk, Is.EqualTo(RiskLevel.Low));
    }

    [Test]
    public void Helper_predicates_and_doctrine_checks()
    {
        Assert.That(AutonomousFailbackPolicy.IsPartitioned(C2NetworkHealthLevel.Partitioned), Is.True);
        Assert.That(AutonomousFailbackPolicy.IsPartitioned(C2NetworkHealthLevel.Healthy), Is.False);
        Assert.That(AutonomousFailbackPolicy.ShouldFailback(C2NetworkHealthLevel.Partitioned), Is.True);
        Assert.That(AutonomousFailbackPolicy.ShouldFailback(C2NetworkHealthLevel.Degraded), Is.False);

        var engageOrder = CreateEngageOrder();
        Assert.That(AutonomousFailbackPolicy.IsOffensiveLethalStrike(engageOrder), Is.True);
        Assert.That(AutonomousFailbackPolicy.IsOffensiveLethalStrike(engageOrder, isPointDefense: true), Is.False);
        Assert.That(AutonomousFailbackPolicy.IsOffensiveLethalStrike(engageOrder, isEmergencySelfDefense: true), Is.False);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. Clean Null Safety
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Null_arguments_throw_ArgumentNullException()
    {
        var policy = new AutonomousFailbackPolicy();

        Assert.Throws<ArgumentNullException>((Action)(() =>
            policy.Evaluate(C2NetworkHealthLevel.Partitioned, (Order)null!)));

        Assert.Throws<ArgumentNullException>((Action)(() =>
            policy.Evaluate((C2NetworkHealthSnapshot)null!, CreateEngageOrder())));

        Assert.Throws<ArgumentNullException>((Action)(() =>
            policy.Evaluate((AutonomousFailbackContext)null!)));

        Assert.Throws<ArgumentNullException>((Action)(() =>
            AutonomousFailbackPolicy.EvaluateContext(null!)));

        Assert.Throws<ArgumentNullException>((Action)(() =>
            AutonomousFailbackPolicy.IsOffensiveLethalStrike(null!)));
    }
}
