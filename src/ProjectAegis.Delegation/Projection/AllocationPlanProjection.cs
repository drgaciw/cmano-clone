namespace ProjectAegis.Delegation.Projection;

using System.Text;
using EmploymentLedger;
using ThreatAssessment;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-410: headless multi-target manual weapon allocation plan.
/// Presentation only (ADR-010 §2–3, ADR-007, ADR-001). Never issues an engage
/// command, never mutates a magazine, and never writes an order log.
/// <see cref="EngageAttackOrderResolver.TryResolve"/> is read and its resolved
/// value is discarded. <see cref="WeaponRecommendation.IsFireOrder"/>,
/// <see cref="WeaponRecommendation.IsWeaponsReleaseAuthorization"/>, and
/// <see cref="WeaponRecommendation.IsAutomaticEngagement"/> are never copied
/// onto <see cref="AllocationPlan.IsFireOrder"/>.
/// </summary>
public static class AllocationPlanProjection
{
    /// <summary>
    /// Projects shooters, targets, and requested quantities into per-cell can-fire
    /// or <see cref="FireAbortReason"/> results, plus shooter and target totals.
    /// Magazine counts come from <see cref="EmploymentLedgerProjection"/> and are
    /// not written back. A null request projects an empty plan.
    /// </summary>
    public static AllocationPlan Project(AllocationPlanRequest? request)
    {
        if (request is null)
        {
            return AllocationPlan.Empty;
        }

        var shooters = IndexIds(request.Shooters, static shooter => shooter.ShooterId);
        var targets = IndexIds(request.Targets, static target => target.TargetId);
        var remaining = IndexMagazines(request.Magazines);
        var allocations = request.Allocations ?? Array.Empty<AllocationPlanAllocation>();
        var order = OrderAllocations(allocations);
        var decisions = new List<CellDecision>(order.Count);
        for (var i = 0; i < order.Count; i++)
        {
            decisions.Add(Evaluate(allocations[order[i]], shooters, targets, remaining));
        }

        var cells = new List<AllocationPlanCell>(decisions.Count);
        for (var i = 0; i < decisions.Count; i++)
        {
            cells.Add(ToCell(decisions[i], remaining));
        }

        return new AllocationPlan(
            cells,
            ShooterTotals(request.Shooters, cells, remaining),
            TargetTotals(request.Targets, cells),
            IsFireOrder: false);
    }

    /// <summary>
    /// Replay-stable fingerprint. Same inputs yield the same string. No wall clock.
    /// </summary>
    public static string Fingerprint(AllocationPlan? plan)
    {
        if (plan is null
            || (plan.Cells.Count == 0 && plan.ShooterTotals.Count == 0 && plan.TargetTotals.Count == 0))
        {
            return "ap:empty";
        }

        var builder = new StringBuilder();
        builder.Append("ap:fire=");
        builder.Append(plan.IsFireOrder ? '1' : '0');
        builder.Append("|c=");
        builder.Append(plan.Cells.Count);
        for (var i = 0; i < plan.Cells.Count; i++)
        {
            AppendCell(builder, plan.Cells[i]);
        }

        builder.Append("|s=");
        builder.Append(plan.ShooterTotals.Count);
        for (var i = 0; i < plan.ShooterTotals.Count; i++)
        {
            var row = plan.ShooterTotals[i];
            builder.Append('|');
            builder.Append(row.ShooterId);
            builder.Append(',');
            builder.Append(row.AcceptedQuantity);
            builder.Append(',');
            builder.Append(row.MagazineRemaining);
        }

        builder.Append("|t=");
        builder.Append(plan.TargetTotals.Count);
        for (var i = 0; i < plan.TargetTotals.Count; i++)
        {
            var row = plan.TargetTotals[i];
            builder.Append('|');
            builder.Append(row.TargetId);
            builder.Append(',');
            builder.Append(row.AcceptedQuantity);
        }

        return builder.ToString();
    }

    private static CellDecision Evaluate(
        AllocationPlanAllocation allocation,
        HashSet<string> shooters,
        HashSet<string> targets,
        Dictionary<MagazineKey, int> remaining)
    {
        var shooterId = allocation.ShooterId;
        var weaponId = allocation.WeaponId;
        var targetId = allocation.TargetId;
        var quantity = allocation.Quantity;
        var preview = allocation.Preview;
        var context = allocation.Context;
        var menuReason = ReadMenuDisabledReason(in context, preview, quantity);

        if (!shooters.Contains(shooterId))
        {
            return Reject(shooterId, weaponId, targetId, quantity, null, "UNKNOWN_SHOOTER");
        }

        if (!targets.Contains(targetId))
        {
            return Reject(shooterId, weaponId, targetId, quantity, null, "UNKNOWN_TARGET");
        }

        if (!preview.CanFire)
        {
            var code = string.IsNullOrWhiteSpace(preview.AbortPreviewCode)
                ? menuReason
                : preview.AbortPreviewCode;
            return RejectMapped(shooterId, weaponId, targetId, quantity, code);
        }

        if (!IsIgnorableMenuReason(menuReason))
        {
            return RejectMapped(shooterId, weaponId, targetId, quantity, menuReason);
        }

        if (TryRecommendationAbort(allocation, out var recommendationReason, out var recommendationCode))
        {
            return Reject(
                shooterId,
                weaponId,
                targetId,
                quantity,
                recommendationReason,
                recommendationCode);
        }

        var key = new MagazineKey(shooterId, weaponId);
        remaining.TryGetValue(key, out var rounds);
        if (quantity <= 0 || quantity > rounds)
        {
            var code = rounds <= 0
                ? AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE
                : AbortReasonCatalog.Engage.NO_AMMO;
            return Reject(shooterId, weaponId, targetId, quantity, null, code);
        }

        remaining[key] = rounds - quantity;
        return Accept(shooterId, weaponId, targetId, quantity, preview);
    }

    /// <summary>
    /// Reads the attack menu and the order resolver. The resolver's order value is
    /// discarded: a legal menu option does not allocate rounds and does not fire.
    /// Ledger quantity is authoritative for magazine shortfalls, so ammo-only menu
    /// failures are ignored here.
    /// </summary>
    private static string? ReadMenuDisabledReason(in EngageContext context, EngagePreview preview, int quantity)
    {
        var options = EngageAttackOptions.Build(in context, preview);
        var optionId = quantity > 1 ? "fire-salvo" : "fire-single";
        string? disabled = null;
        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            if (!string.Equals(option.Id, optionId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!option.Enabled)
            {
                disabled = option.DisabledReason;
            }

            break;
        }

        EngageAttackOrderResolver.TryResolve(
            optionId,
            in context,
            preview,
            out _,
            out var failureReason);

        if (TryMapAbort(disabled, out _))
        {
            return disabled;
        }

        if (TryMapAbort(failureReason, out _))
        {
            return failureReason;
        }

        return disabled ?? failureReason;
    }

    private static bool TryRecommendationAbort(
        AllocationPlanAllocation allocation,
        out FireAbortReason? reason,
        out string? code)
    {
        reason = null;
        code = null;
        var recommendation = allocation.Recommendation;
        if (recommendation is null || !MatchesAllocation(allocation, recommendation))
        {
            return false;
        }

        if (recommendation.Outcome is not (WeaponRecommendationOutcome.WithheldByPolicy
            or WeaponRecommendationOutcome.WithheldByEngage))
        {
            return false;
        }

        if (!TryMapAbort(recommendation.WithheldReasonCode, out var mapped))
        {
            code = string.IsNullOrWhiteSpace(recommendation.WithheldReasonCode)
                ? recommendation.Outcome.ToString()
                : recommendation.WithheldReasonCode;
            return true;
        }

        reason = mapped;
        code = CanonicalReasonCode(recommendation.WithheldReasonCode, mapped);
        return true;
    }

    private static bool MatchesAllocation(
        AllocationPlanAllocation allocation,
        WeaponRecommendation recommendation)
    {
        if (!string.Equals(allocation.ShooterId, recommendation.ShooterUnitId, StringComparison.Ordinal)
            || !string.Equals(allocation.WeaponId, recommendation.WeaponId, StringComparison.Ordinal))
        {
            return false;
        }

        var recommendedTarget = string.IsNullOrEmpty(recommendation.TargetId)
            ? recommendation.ContactId
            : recommendation.TargetId;
        return string.Equals(allocation.TargetId, recommendedTarget, StringComparison.Ordinal);
    }

    private static bool IsIgnorableMenuReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason)
        || string.Equals(reason, "BLOCKED", StringComparison.Ordinal)
        || string.Equals(reason, "UNKNOWN_OPTION", StringComparison.Ordinal)
        || string.Equals(reason, AbortReasonCatalog.Engage.NO_AMMO, StringComparison.Ordinal)
        || string.Equals(reason, AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE, StringComparison.Ordinal);

    private static CellDecision RejectMapped(
        string shooterId,
        string weaponId,
        string targetId,
        int quantity,
        string? code)
    {
        FireAbortReason? reason = null;
        var canonical = string.IsNullOrWhiteSpace(code) ? null : code;
        if (TryMapAbort(code, out var mapped))
        {
            reason = mapped;
            canonical = CanonicalReasonCode(code, mapped);
        }

        return Reject(shooterId, weaponId, targetId, quantity, reason, canonical);
    }

    private static CellDecision Reject(
        string shooterId,
        string weaponId,
        string targetId,
        int quantity,
        FireAbortReason? reason,
        string? code) =>
        new(
            shooterId,
            weaponId,
            targetId,
            quantity,
            AcceptedQuantity: 0,
            CanFire: false,
            reason,
            string.IsNullOrWhiteSpace(code) ? null : code,
            EngageExplainProjection.ExplainCode(code));

    private static CellDecision Accept(
        string shooterId,
        string weaponId,
        string targetId,
        int quantity,
        EngagePreview preview) =>
        new(
            shooterId,
            weaponId,
            targetId,
            quantity,
            quantity,
            CanFire: true,
            AbortReason: null,
            ReasonCode: null,
            EngageExplainProjection.Project(preview).ReasonPlain);

    private static bool TryMapAbort(string? code, out FireAbortReason reason)
    {
        reason = FireAbortReason.None;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (Enum.TryParse(code, ignoreCase: false, out FireAbortReason parsed)
            && parsed != FireAbortReason.None
            && Enum.IsDefined(typeof(FireAbortReason), parsed))
        {
            reason = parsed;
            return true;
        }

        var mapped = code switch
        {
            AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE => FireAbortReason.RoeHoldFire,
            AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT => FireAbortReason.WeaponsTight,
            AbortReasonCatalog.Doctrine.WRA_RANGE
                or AbortReasonCatalog.Engage.DLZ_OUT
                or AbortReasonCatalog.Engage.OUT_OF_ENVELOPE => FireAbortReason.WraRange,
            AbortReasonCatalog.Doctrine.WRA_SALVO => FireAbortReason.WraSalvo,
            AbortReasonCatalog.Doctrine.EMCON_OFF => FireAbortReason.EmconOff,
            AbortReasonCatalog.Doctrine.NO_FIRE_CONTROL_TRACK => FireAbortReason.NoFireControlTrack,
            AbortReasonCatalog.Doctrine.COMMS_DENIED => FireAbortReason.CommsDenied,
            AbortReasonCatalog.Doctrine.COMMS_OFF_GRID => FireAbortReason.OffGrid,
            AbortReasonCatalog.Doctrine.AIR_ASPECT_BLOCK => FireAbortReason.AirAspectBlock,
            AbortReasonCatalog.Doctrine.SURFACE_ASPECT_BLOCK => FireAbortReason.SurfaceAspectBlock,
            AbortReasonCatalog.Doctrine.SUBSURFACE_ASPECT_BLOCK => FireAbortReason.SubsurfaceAspectBlock,
            AbortReasonCatalog.Doctrine.LAND_ASPECT_BLOCK => FireAbortReason.LandAspectBlock,
            AbortReasonCatalog.Doctrine.MINE_ASPECT_BLOCK => FireAbortReason.MineAspectBlock,
            AbortReasonCatalog.Doctrine.FACILITY_ASPECT_BLOCK => FireAbortReason.FacilityAspectBlock,
            AbortReasonCatalog.Doctrine.AUTO_ENGAGE_DENIED => FireAbortReason.AutoEngageDenied,
            AbortReasonCatalog.Doctrine.EXPEND_UNAUTHORIZED => FireAbortReason.ExpendUnauthorized,
            _ => FireAbortReason.None,
        };

        if (mapped == FireAbortReason.None)
        {
            return false;
        }

        reason = mapped;
        return true;
    }

    private static string? CanonicalReasonCode(string? code, FireAbortReason reason)
    {
        if (!string.IsNullOrWhiteSpace(code)
            && !Enum.TryParse(code, ignoreCase: false, out FireAbortReason _))
        {
            return code;
        }

        return reason switch
        {
            FireAbortReason.RoeHoldFire => AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE,
            FireAbortReason.WeaponsTight => AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT,
            FireAbortReason.WraRange => AbortReasonCatalog.Doctrine.WRA_RANGE,
            FireAbortReason.WraSalvo => AbortReasonCatalog.Doctrine.WRA_SALVO,
            FireAbortReason.EmconOff => AbortReasonCatalog.Doctrine.EMCON_OFF,
            FireAbortReason.NoFireControlTrack => AbortReasonCatalog.Doctrine.NO_FIRE_CONTROL_TRACK,
            FireAbortReason.CommsDenied => AbortReasonCatalog.Doctrine.COMMS_DENIED,
            FireAbortReason.OffGrid => AbortReasonCatalog.Doctrine.COMMS_OFF_GRID,
            FireAbortReason.AirAspectBlock => AbortReasonCatalog.Doctrine.AIR_ASPECT_BLOCK,
            FireAbortReason.SurfaceAspectBlock => AbortReasonCatalog.Doctrine.SURFACE_ASPECT_BLOCK,
            FireAbortReason.SubsurfaceAspectBlock => AbortReasonCatalog.Doctrine.SUBSURFACE_ASPECT_BLOCK,
            FireAbortReason.LandAspectBlock => AbortReasonCatalog.Doctrine.LAND_ASPECT_BLOCK,
            FireAbortReason.MineAspectBlock => AbortReasonCatalog.Doctrine.MINE_ASPECT_BLOCK,
            FireAbortReason.FacilityAspectBlock => AbortReasonCatalog.Doctrine.FACILITY_ASPECT_BLOCK,
            FireAbortReason.AutoEngageDenied => AbortReasonCatalog.Doctrine.AUTO_ENGAGE_DENIED,
            FireAbortReason.ExpendUnauthorized => AbortReasonCatalog.Doctrine.EXPEND_UNAUTHORIZED,
            _ => string.IsNullOrWhiteSpace(code) ? null : code,
        };
    }

    private static Dictionary<MagazineKey, int> IndexMagazines(
        IReadOnlyList<EmploymentLedgerMagazineFacts>? magazines)
    {
        var snapshot = EmploymentLedgerProjection.Project(magazines);
        var map = new Dictionary<MagazineKey, int>();
        for (var i = 0; i < snapshot.Rows.Count; i++)
        {
            var row = snapshot.Rows[i];
            var rounds = row.RoundsRemaining < 0 ? 0 : row.RoundsRemaining;
            var key = new MagazineKey(row.ShooterId, row.WeaponFamilyId);
            map.TryGetValue(key, out var current);
            map[key] = current + rounds;
        }

        return map;
    }

    private static HashSet<string> IndexIds<T>(IReadOnlyList<T>? rows, Func<T, string> id)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (rows is null)
        {
            return set;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row is null)
            {
                continue;
            }

            var value = id(row);
            if (!string.IsNullOrEmpty(value))
            {
                set.Add(value);
            }
        }

        return set;
    }

    private static List<int> OrderAllocations(IReadOnlyList<AllocationPlanAllocation> allocations)
    {
        var order = new List<int>(allocations.Count);
        for (var i = 0; i < allocations.Count; i++)
        {
            order.Add(i);
        }

        order.Sort((left, right) => CompareAllocation(allocations[left], allocations[right], left, right));
        return order;
    }

    private static int CompareAllocation(
        AllocationPlanAllocation left,
        AllocationPlanAllocation right,
        int leftIndex,
        int rightIndex)
    {
        var shooter = string.Compare(left.ShooterId, right.ShooterId, StringComparison.Ordinal);
        if (shooter != 0)
        {
            return shooter;
        }

        var weapon = string.Compare(left.WeaponId, right.WeaponId, StringComparison.Ordinal);
        if (weapon != 0)
        {
            return weapon;
        }

        var target = string.Compare(left.TargetId, right.TargetId, StringComparison.Ordinal);
        if (target != 0)
        {
            return target;
        }

        return leftIndex.CompareTo(rightIndex);
    }

    private static AllocationPlanCell ToCell(
        CellDecision decision,
        Dictionary<MagazineKey, int> remaining)
    {
        remaining.TryGetValue(new MagazineKey(decision.ShooterId, decision.WeaponId), out var rounds);
        return new AllocationPlanCell(
            decision.ShooterId,
            decision.WeaponId,
            decision.TargetId,
            decision.Quantity,
            decision.AcceptedQuantity,
            decision.CanFire,
            decision.AbortReason,
            decision.ReasonCode,
            decision.ReasonText,
            rounds);
    }

    private static IReadOnlyList<AllocationPlanShooterTotal> ShooterTotals(
        IReadOnlyList<AllocationPlanShooter>? shooters,
        IReadOnlyList<AllocationPlanCell> cells,
        Dictionary<MagazineKey, int> remaining)
    {
        var ids = DistinctSorted(shooters, static shooter => shooter.ShooterId);
        var totals = new List<AllocationPlanShooterTotal>(ids.Count);
        for (var i = 0; i < ids.Count; i++)
        {
            totals.Add(new AllocationPlanShooterTotal(
                ids[i],
                AcceptedFor(cells, ids[i], target: false),
                RemainingForShooter(ids[i], cells, remaining)));
        }

        return totals;
    }

    private static IReadOnlyList<AllocationPlanTargetTotal> TargetTotals(
        IReadOnlyList<AllocationPlanTarget>? targets,
        IReadOnlyList<AllocationPlanCell> cells)
    {
        var ids = DistinctSorted(targets, static target => target.TargetId);
        var totals = new List<AllocationPlanTargetTotal>(ids.Count);
        for (var i = 0; i < ids.Count; i++)
        {
            totals.Add(new AllocationPlanTargetTotal(
                ids[i],
                AcceptedFor(cells, ids[i], target: true)));
        }

        return totals;
    }

    private static List<string> DistinctSorted<T>(IReadOnlyList<T>? rows, Func<T, string?> id)
    {
        var ids = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (rows is null)
        {
            return ids;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row is null)
            {
                continue;
            }

            var value = id(row);
            if (!string.IsNullOrEmpty(value) && seen.Add(value))
            {
                ids.Add(value);
            }
        }

        ids.Sort(StringComparer.Ordinal);
        return ids;
    }

    private static int AcceptedFor(IReadOnlyList<AllocationPlanCell> cells, string id, bool target)
    {
        var sum = 0;
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            var key = target ? cell.TargetId : cell.ShooterId;
            if (string.Equals(key, id, StringComparison.Ordinal))
            {
                sum += cell.AcceptedQuantity;
            }
        }

        return sum;
    }

    private static int RemainingForShooter(
        string shooterId,
        IReadOnlyList<AllocationPlanCell> cells,
        Dictionary<MagazineKey, int> remaining)
    {
        var weapons = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var key in remaining.Keys)
        {
            if (string.Equals(key.ShooterId, shooterId, StringComparison.Ordinal))
            {
                weapons.Add(key.WeaponId);
            }
        }

        for (var i = 0; i < cells.Count; i++)
        {
            if (string.Equals(cells[i].ShooterId, shooterId, StringComparison.Ordinal))
            {
                weapons.Add(cells[i].WeaponId);
            }
        }

        var sum = 0;
        foreach (var weapon in weapons)
        {
            if (remaining.TryGetValue(new MagazineKey(shooterId, weapon), out var rounds))
            {
                sum += rounds;
            }
        }

        return sum;
    }

    private static void AppendCell(StringBuilder builder, AllocationPlanCell cell)
    {
        builder.Append('|');
        builder.Append(cell.ShooterId);
        builder.Append(',');
        builder.Append(cell.WeaponId);
        builder.Append(',');
        builder.Append(cell.TargetId);
        builder.Append(',');
        builder.Append(cell.Quantity);
        builder.Append(',');
        builder.Append(cell.AcceptedQuantity);
        builder.Append(',');
        builder.Append(cell.CanFire ? '1' : '0');
        builder.Append(',');
        builder.Append(cell.AbortReason?.ToString() ?? string.Empty);
        builder.Append(',');
        builder.Append(cell.ReasonCode ?? string.Empty);
        builder.Append(',');
        builder.Append(cell.MagazineRemaining);
        builder.Append(',');
        builder.Append(cell.ReasonText.Length);
        builder.Append(':');
        builder.Append(cell.ReasonText);
    }

    private readonly record struct MagazineKey(string ShooterId, string WeaponId);

    private readonly record struct CellDecision(
        string ShooterId,
        string WeaponId,
        string TargetId,
        int Quantity,
        int AcceptedQuantity,
        bool CanFire,
        FireAbortReason? AbortReason,
        string? ReasonCode,
        string ReasonText);
}

/// <summary>Selected shooter in a manual allocation plan. Identity only; engage facts live on each allocation.</summary>
public sealed record AllocationPlanShooter(string ShooterId);

/// <summary>Selected target in a manual allocation plan.</summary>
public sealed record AllocationPlanTarget(string TargetId);

/// <summary>
/// One requested quantity of a weapon from a shooter onto a target.
/// <paramref name="Recommendation"/> is advisory and cannot authorize release.
/// </summary>
public sealed record AllocationPlanAllocation(
    string ShooterId,
    string WeaponId,
    string TargetId,
    int Quantity,
    EngageContext Context,
    EngagePreview Preview,
    WeaponRecommendation? Recommendation = null);

/// <summary>Inputs for <see cref="AllocationPlanProjection"/>. Null lists are treated as empty.</summary>
public sealed record AllocationPlanRequest(
    IReadOnlyList<AllocationPlanShooter>? Shooters,
    IReadOnlyList<AllocationPlanTarget>? Targets,
    IReadOnlyList<AllocationPlanAllocation>? Allocations,
    IReadOnlyList<EmploymentLedgerMagazineFacts>? Magazines);

/// <summary>
/// One shooter / weapon / target row. <see cref="CanFire"/> is the clear result.
/// Otherwise <see cref="AbortReason"/> is the domain code when the failure maps onto
/// <see cref="FireAbortReason"/>, and <see cref="ReasonCode"/> is the catalog or ledger code.
/// Reason text comes from <see cref="EngageExplainProjection"/>.
/// </summary>
public sealed record AllocationPlanCell(
    string ShooterId,
    string WeaponId,
    string TargetId,
    int Quantity,
    int AcceptedQuantity,
    bool CanFire,
    FireAbortReason? AbortReason,
    string? ReasonCode,
    string ReasonText,
    int MagazineRemaining);

/// <summary>Accepted rounds and remaining magazine count for one shooter, summed across that shooter's weapons.</summary>
public sealed record AllocationPlanShooterTotal(
    string ShooterId,
    int AcceptedQuantity,
    int MagazineRemaining);

/// <summary>Accepted rounds allocated onto one target.</summary>
public sealed record AllocationPlanTargetTotal(string TargetId, int AcceptedQuantity);

/// <summary>
/// Deterministic allocation preview. <see cref="IsFireOrder"/> is false for every plan
/// produced by <see cref="AllocationPlanProjection.Project"/>.
/// </summary>
public sealed record AllocationPlan(
    IReadOnlyList<AllocationPlanCell> Cells,
    IReadOnlyList<AllocationPlanShooterTotal> ShooterTotals,
    IReadOnlyList<AllocationPlanTargetTotal> TargetTotals,
    bool IsFireOrder)
{
    /// <summary>Empty advisory plan. Not a fire order.</summary>
    public static AllocationPlan Empty { get; } = new(
        Array.Empty<AllocationPlanCell>(),
        Array.Empty<AllocationPlanShooterTotal>(),
        Array.Empty<AllocationPlanTargetTotal>(),
        IsFireOrder: false);
}
