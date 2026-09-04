namespace ProjectAegis.Delegation.Orchestration;

using System;
using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Sim.Policy;

/// <summary>
/// Autonomous failback mode applied when a unit or task group becomes partitioned
/// from its commanding C2 node per Draft 25 (C2N-02 / AEGIS-307 / DRG-237).
/// </summary>
public enum AutonomousFailbackMode
{
    /// <summary>Cease offensive lethal fires; preserve point-defense and survivability.</summary>
    HoldFire = 0,

    /// <summary>Retrograde/transit to home station; defend self in transit.</summary>
    ReturnToBase = 1,

    /// <summary>Alias for <see cref="ReturnToBase"/>.</summary>
    Rtb = 1,

    /// <summary>Uppercase alias for <see cref="ReturnToBase"/>.</summary>
    RTB = 1,

    /// <summary>Execute according to pre-briefed local mission package ROE parameters.</summary>
    ExecuteLocalPackageRoe = 2,
}

/// <summary>
/// Verdict resulting from autonomous failback policy evaluation.
/// </summary>
public sealed record AutonomousFailbackResult(
    bool Allowed,
    FireAbortReason DenialReason = FireAbortReason.None,
    string? Explanation = null,
    AutonomousFailbackMode Mode = AutonomousFailbackMode.HoldFire)
{
    public bool Denied => !Allowed;

    public bool IsAllowed => Allowed;

    public static AutonomousFailbackResult Allow(
        AutonomousFailbackMode mode = AutonomousFailbackMode.HoldFire,
        string? explanation = null) =>
        new(true, FireAbortReason.None, explanation, mode);

    public static AutonomousFailbackResult Deny(
        FireAbortReason reason,
        AutonomousFailbackMode mode = AutonomousFailbackMode.HoldFire,
        string? explanation = null) =>
        new(false, reason, explanation, mode);
}

/// <summary>
/// Context parameters for autonomous failback evaluation.
/// </summary>
public sealed record AutonomousFailbackContext(
    C2NetworkHealthLevel NetworkHealth,
    Order? Order = null,
    OrderKind? OrderKind = null,
    RiskLevel Risk = RiskLevel.Low,
    AutonomousFailbackMode Mode = AutonomousFailbackMode.HoldFire,
    bool IsPointDefense = false,
    bool IsEmergencySelfDefense = false,
    bool IsEmergencySurvivability = false,
    bool LocalPackageRoePermits = false,
    IRoeFilter? LocalRoeFilter = null);

/// <summary>
/// C2 node partitioning autonomous failback policy per Draft 25 (C2N-02 / AEGIS-307 / DRG-237).
/// Restricts offensive lethal strikes from severed commanding nodes during C2 network partition,
/// while preserving local point-defense and emergency survivability.
/// </summary>
public sealed class AutonomousFailbackPolicy
{
    public AutonomousFailbackMode DefaultMode { get; set; }

    public IRoeFilter? LocalRoeFilter { get; set; }

    public AutonomousFailbackPolicy(
        AutonomousFailbackMode defaultMode = AutonomousFailbackMode.HoldFire,
        IRoeFilter? localRoeFilter = null)
    {
        DefaultMode = defaultMode;
        LocalRoeFilter = localRoeFilter;
    }

    public static bool IsPartitioned(C2NetworkHealthLevel health) =>
        health == C2NetworkHealthLevel.Partitioned;

    public static bool ShouldFailback(C2NetworkHealthLevel health) =>
        health == C2NetworkHealthLevel.Partitioned;

    public AutonomousFailbackResult Evaluate(
        C2NetworkHealthLevel networkHealth,
        Order order,
        bool isPointDefense = false,
        bool isEmergencySelfDefense = false,
        bool isEmergencySurvivability = false,
        bool localPackageRoePermits = false)
    {
        return Evaluate(
            DefaultMode,
            networkHealth,
            order,
            isPointDefense,
            isEmergencySelfDefense,
            isEmergencySurvivability,
            localPackageRoePermits,
            LocalRoeFilter);
    }

    public AutonomousFailbackResult Evaluate(
        C2NetworkHealthSnapshot snapshot,
        Order order,
        bool isPointDefense = false,
        bool isEmergencySelfDefense = false,
        bool isEmergencySurvivability = false,
        bool localPackageRoePermits = false)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return Evaluate(
            DefaultMode,
            snapshot.NetworkHealth,
            order,
            isPointDefense,
            isEmergencySelfDefense,
            isEmergencySurvivability,
            localPackageRoePermits,
            LocalRoeFilter);
    }

    public AutonomousFailbackResult Evaluate(
        C2NetworkHealthLevel networkHealth,
        OrderKind orderKind,
        RiskLevel risk = RiskLevel.Low,
        bool isPointDefense = false,
        bool isEmergencySelfDefense = false,
        bool isEmergencySurvivability = false,
        bool localPackageRoePermits = false)
    {
        return Evaluate(
            DefaultMode,
            networkHealth,
            orderKind,
            risk,
            isPointDefense,
            isEmergencySelfDefense,
            isEmergencySurvivability,
            localPackageRoePermits,
            LocalRoeFilter);
    }

    public AutonomousFailbackResult Evaluate(AutonomousFailbackContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var effectiveFilter = context.LocalRoeFilter ?? LocalRoeFilter;

        if (context.Order is not null)
        {
            return Evaluate(
                context.Mode,
                context.NetworkHealth,
                context.Order,
                context.IsPointDefense,
                context.IsEmergencySelfDefense,
                context.IsEmergencySurvivability,
                context.LocalPackageRoePermits,
                effectiveFilter);
        }

        var kind = context.OrderKind ?? OrderKind.Hold;
        return Evaluate(
            context.Mode,
            context.NetworkHealth,
            kind,
            context.Risk,
            context.IsPointDefense,
            context.IsEmergencySelfDefense,
            context.IsEmergencySurvivability,
            context.LocalPackageRoePermits,
            effectiveFilter);
    }

    public static AutonomousFailbackResult EvaluateContext(AutonomousFailbackContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Order is not null)
        {
            return Evaluate(
                context.Mode,
                context.NetworkHealth,
                context.Order,
                context.IsPointDefense,
                context.IsEmergencySelfDefense,
                context.IsEmergencySurvivability,
                context.LocalPackageRoePermits,
                context.LocalRoeFilter);
        }

        var kind = context.OrderKind ?? OrderKind.Hold;
        return Evaluate(
            context.Mode,
            context.NetworkHealth,
            kind,
            context.Risk,
            context.IsPointDefense,
            context.IsEmergencySelfDefense,
            context.IsEmergencySurvivability,
            context.LocalPackageRoePermits,
            context.LocalRoeFilter);
    }

    public static AutonomousFailbackResult Evaluate(
        AutonomousFailbackMode mode,
        C2NetworkHealthSnapshot snapshot,
        Order order,
        bool isPointDefense = false,
        bool isEmergencySelfDefense = false,
        bool isEmergencySurvivability = false,
        bool localPackageRoePermits = false,
        IRoeFilter? localRoeFilter = null)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return Evaluate(
            mode,
            snapshot.NetworkHealth,
            order,
            isPointDefense,
            isEmergencySelfDefense,
            isEmergencySurvivability,
            localPackageRoePermits,
            localRoeFilter);
    }

    public static AutonomousFailbackResult Evaluate(
        AutonomousFailbackMode mode,
        C2NetworkHealthLevel networkHealth,
        Order order,
        bool isPointDefense = false,
        bool isEmergencySelfDefense = false,
        bool isEmergencySurvivability = false,
        bool localPackageRoePermits = false,
        IRoeFilter? localRoeFilter = null)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        // 1. Healthy / Degraded mesh states allow normal command execution.
        if (networkHealth != C2NetworkHealthLevel.Partitioned)
        {
            return AutonomousFailbackResult.Allow(mode, "C2 network health allows normal command execution.");
        }

        // 2. Preserved local point-defense & emergency survivability.
        if (IsPointDefenseOrEmergency(isPointDefense, isEmergencySelfDefense, isEmergencySurvivability))
        {
            return AutonomousFailbackResult.Allow(mode, "Local point-defense and emergency survivability preserved during C2 partition.");
        }

        // 3. Check if order is offensive lethal strike.
        var isLethal = AutonomyGate.IsLethalFireOrder(order);

        // 4. Apply mode-specific partitioning failback behavior.
        return ApplyPartitionedDoctrine(
            mode,
            order,
            isLethal,
            localPackageRoePermits,
            localRoeFilter);
    }

    public static AutonomousFailbackResult Evaluate(
        AutonomousFailbackMode mode,
        C2NetworkHealthLevel networkHealth,
        OrderKind orderKind,
        RiskLevel risk = RiskLevel.Low,
        bool isPointDefense = false,
        bool isEmergencySelfDefense = false,
        bool isEmergencySurvivability = false,
        bool localPackageRoePermits = false,
        IRoeFilter? localRoeFilter = null)
    {
        if (networkHealth != C2NetworkHealthLevel.Partitioned)
        {
            return AutonomousFailbackResult.Allow(mode, "C2 network health allows normal command execution.");
        }

        if (IsPointDefenseOrEmergency(isPointDefense, isEmergencySelfDefense, isEmergencySurvivability))
        {
            return AutonomousFailbackResult.Allow(mode, "Local point-defense and emergency survivability preserved during C2 partition.");
        }

        var isLethal = orderKind == OrderKind.Engage || risk == RiskLevel.High;

        return ApplyPartitionedDoctrine(
            mode,
            order: null,
            isLethal: isLethal,
            localPackageRoePermits: localPackageRoePermits,
            localRoeFilter: localRoeFilter,
            kind: orderKind);
    }

    private static AutonomousFailbackResult ApplyPartitionedDoctrine(
        AutonomousFailbackMode mode,
        Order? order,
        bool isLethal,
        bool localPackageRoePermits,
        IRoeFilter? localRoeFilter,
        OrderKind? kind = null)
    {
        switch (mode)
        {
            case AutonomousFailbackMode.HoldFire:
                if (isLethal)
                {
                    return AutonomousFailbackResult.Deny(
                        FireAbortReason.RoeHoldFire,
                        mode,
                        "C2 node partitioned: autonomous failback HoldFire restricts offensive lethal strikes.");
                }
                return AutonomousFailbackResult.Allow(mode, "Non-lethal command permitted under failback HoldFire.");

            case AutonomousFailbackMode.ReturnToBase:
                if (isLethal)
                {
                    return AutonomousFailbackResult.Deny(
                        FireAbortReason.CommsDenied,
                        mode,
                        "C2 node partitioned: autonomous failback ReturnToBase restricts offensive lethal strikes.");
                }
                return AutonomousFailbackResult.Allow(mode, "Maneuver / RTB permitted under failback ReturnToBase.");

            case AutonomousFailbackMode.ExecuteLocalPackageRoe:
                if (isLethal)
                {
                    if (localRoeFilter is not null && order is not null)
                    {
                        var roe = localRoeFilter.Evaluate(order);
                        if (roe.Verdict == RoeVerdict.Allow)
                        {
                            return AutonomousFailbackResult.Allow(mode, "Local package ROE permits engagement.");
                        }

                        var reason = roe.Reason != FireAbortReason.None
                            ? roe.Reason
                            : FireAbortReason.RoeHoldFire;

                        return AutonomousFailbackResult.Deny(
                            reason,
                            mode,
                            "Local package ROE restricts engagement during C2 partition.");
                    }

                    if (localPackageRoePermits)
                    {
                        return AutonomousFailbackResult.Allow(mode, "Local package ROE permits engagement.");
                    }

                    return AutonomousFailbackResult.Deny(
                        FireAbortReason.RoeHoldFire,
                        mode,
                        "Local package ROE does not authorize offensive lethal strike during C2 partition.");
                }
                return AutonomousFailbackResult.Allow(mode, "Command permitted under local package ROE doctrine.");

            default:
                if (isLethal)
                {
                    return AutonomousFailbackResult.Deny(
                        FireAbortReason.RoeHoldFire,
                        mode,
                        "C2 node partitioned: unhandled failback mode restricts offensive lethal strikes.");
                }
                return AutonomousFailbackResult.Allow(mode, "Non-lethal command permitted under failback.");
        }
    }

    public static bool IsPointDefenseOrEmergency(
        bool isPointDefense,
        bool isEmergencySelfDefense,
        bool isEmergencySurvivability = false) =>
        isPointDefense || isEmergencySelfDefense || isEmergencySurvivability;

    public static bool IsOffensiveLethalStrike(
        Order order,
        bool isPointDefense = false,
        bool isEmergencySelfDefense = false,
        bool isEmergencySurvivability = false)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        if (IsPointDefenseOrEmergency(isPointDefense, isEmergencySelfDefense, isEmergencySurvivability))
        {
            return false;
        }

        return AutonomyGate.IsLethalFireOrder(order);
    }

    public static OrderKind GetFailbackOrderKind(AutonomousFailbackMode mode) =>
        mode switch
        {
            AutonomousFailbackMode.ReturnToBase => OrderKind.ReturnToBase,
            AutonomousFailbackMode.HoldFire => OrderKind.Hold,
            AutonomousFailbackMode.ExecuteLocalPackageRoe => OrderKind.Hold,
            _ => OrderKind.Hold,
        };

    public static Order CreateFailbackOrder(
        OrderId orderId,
        TargetId target,
        double simTime,
        AutonomousFailbackMode mode)
    {
        var kind = GetFailbackOrderKind(mode);
        return new Order(orderId, target, simTime, kind, RiskLevel.Low);
    }
}
