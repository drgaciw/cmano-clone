namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.EmconPosture;
using ProjectAegis.Delegation.PlatformDegrade;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Policy;

/// <summary>Explicit knowledge state for a status fact. Unknown is never treated as healthy.</summary>
public enum StatusKnowledge { Unknown = 0, Nominal = 1, Degraded = 2, Offline = 3 }

/// <summary>One source-labelled semantic status fact.</summary>
public sealed record StatusFact(StatusKnowledge State, string Detail, ulong? SourceSequenceId = null, double? SourceSimTime = null)
{
    public static StatusFact Unknown(string subject) => new(StatusKnowledge.Unknown, $"{subject}: UNKNOWN");
    public static StatusFact Nominal(string detail) => new(StatusKnowledge.Nominal, detail);
}

/// <summary>Authoritative optional per-unit component facts supplied by the simulation snapshot host.</summary>
public sealed record StatusUnitFacts(
    string UnitId, StatusFact Platform, StatusFact Sensors, StatusFact Mounts,
    StatusFact Comms, StatusFact Mobility, StatusFact Readiness, StatusFact Recovery)
{
    public StatusUnitFacts(string unitId, StatusKnowledge platform, StatusKnowledge sensors, StatusKnowledge mounts,
        StatusKnowledge comms, StatusKnowledge mobility, StatusFact recovery)
        : this(unitId, From(platform, "platform"), From(sensors, "sensors"), From(mounts, "mounts"),
            From(comms, "comms"), From(mobility, "mobility"), StatusFact.Unknown("readiness"), recovery) { }

    private static StatusFact From(StatusKnowledge state, string subject) =>
        state == StatusKnowledge.Unknown ? StatusFact.Unknown(subject) : new StatusFact(state, $"{subject}: {state.ToString().ToUpperInvariant()}");
}

/// <summary>Optional current sensor inventory and actual emitter state for one own unit.</summary>
public sealed record StatusSensorFacts(string UnitId, EmconState EmconLevel, IReadOnlyList<string> ActiveEmitterIds,
    double? SourceSimTime = null);

public interface IStatusUnitSource { bool TryGetUnitStatus(string unitId, out StatusUnitFacts facts); }
public interface IStatusSensorSource { bool TryGetSensorStatus(string unitId, out StatusSensorFacts facts); }
public interface IStatusElectronicWarfareSource { bool TryGetElectronicWarfareStatus(string unitId, out StatusFact fact); }

/// <summary>Contact sensor provenance, confidence and freshness copied without UI inference.</summary>
public sealed record StatusContactRow(string ContactId, string TargetId, string ObserverId, string SourceRef,
    ContactProvenanceConfidence Confidence, ContactProvenanceFreshness Freshness, ulong AgeTicks,
    bool IsOutOfComms, ContactProvenanceQualityState QualityState, ulong SourceSimTick, double SourceSimTime);

/// <summary>Own-unit system status. Recovery remains separate because no repair model may be inferred.</summary>
public sealed record StatusUnitRow(string UnitId, StatusFact Platform, StatusFact Sensors, StatusFact Mounts,
    StatusFact Comms, StatusFact Mobility, StatusFact Readiness, StatusFact Recovery);

/// <summary>Known active emissions for a unit, or explicit unknown availability.</summary>
public sealed record StatusEmissionRow(string UnitId, StatusKnowledge State, EmconState? EmconLevel,
    IReadOnlyList<string> KnownEmitters, string Detail);

/// <summary>Known EW state for a unit, or explicit unknown availability.</summary>
public sealed record StatusElectronicWarfareRow(string UnitId, StatusKnowledge State, string Detail);

/// <summary>Sequence-authoritative status-change correlation copied from the decision log.</summary>
public sealed record StatusCorrelationRow(ulong SequenceId, double SimTime, string UnitId, string Kind, string Detail);

/// <summary>Immutable tick-level Slice C status read model.</summary>
public sealed record StatusFrame(double SimTime, IReadOnlyList<StatusContactRow> Contacts,
    IReadOnlyList<StatusUnitRow> Units, IReadOnlyList<StatusEmissionRow> Emissions,
    IReadOnlyList<StatusElectronicWarfareRow> ElectronicWarfare, PlatformDegradeSnapshot? PlatformDegradation,
    IReadOnlyList<StatusCorrelationRow> Correlations);

/// <summary>Builds Slice C status solely from snapshot capabilities, existing projections and authoritative log entries.</summary>
public static class StatusFrameBridge
{
    public static StatusFrame Build(DelegationBridge bridge, ISimWorldSnapshot snapshot, SliceAContactFrame contacts)
    {
        if (bridge is null) throw new ArgumentNullException(nameof(bridge));
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        if (contacts is null) throw new ArgumentNullException(nameof(contacts));
        if (double.IsNaN(snapshot.SimTime) || double.IsInfinity(snapshot.SimTime) || snapshot.SimTime < 0)
            throw new ArgumentOutOfRangeException(nameof(snapshot), "Snapshot simulation time must be finite and non-negative.");
        if (contacts.SimTime is { } contactTime)
        {
            if (double.IsNaN(contactTime) || double.IsInfinity(contactTime) || contactTime < 0)
                throw new ArgumentException("Contact frame simulation time must be finite and non-negative.", nameof(contacts));
            if (contactTime > snapshot.SimTime)
                throw new ArgumentException("Contact frame cannot be newer than the world snapshot.", nameof(contacts));
        }
        if (contacts.SimTick > (ulong)snapshot.SimTime)
            throw new ArgumentException("Contact frame tick cannot be newer than the world snapshot tick.", nameof(contacts));

        var log = bridge.Orchestrator.DecisionLog;
        var contactRows = contacts.Provenance.Contacts.Select(p => new StatusContactRow(
            p.ContactId, p.Source.TargetId, p.Source.ObserverId, p.Source.SourceRef, p.Confidence, p.Freshness,
            p.AgeTicks, p.OutOfCommsUnknown, p.QualityState, p.LastKnown.LastSimTick, p.LastKnown.LastSimTime)).ToArray();

        var unitRows = new List<StatusUnitRow>();
        var emissionRows = new List<StatusEmissionRow>();
        var ewRows = new List<StatusElectronicWarfareRow>();
        foreach (var id in bridge.Registry.CollectMemberIds().Select(x => x.Value).OrderBy(x => x, StringComparer.Ordinal))
        {
            var supplied = snapshot is IStatusUnitSource source && source.TryGetUnitStatus(id, out var facts)
                ? ValidateUnitFacts(id, facts, snapshot.SimTime) : UnknownUnit(id);
            var damages = log.PlatformDamageChanges.Where(x => x.UnitId.Value == id && x.SimTime <= snapshot.SimTime)
                .OrderBy(x => x.SequenceId).ToArray();
            var damage = damages.LastOrDefault();
            var comms = log.CommsStateChanges.Where(x => x.NodeId == id && x.SimTime <= snapshot.SimTime)
                .OrderBy(x => x.SequenceId).LastOrDefault();
            var readiness = bridge.Session?.UnitReadiness is { } ready && ready.IsTracked(id)
                ? new StatusFact(ready.IsReadyForLaunch(id) ? StatusKnowledge.Nominal : StatusKnowledge.Offline,
                    ready.IsReadyForLaunch(id) ? "readiness: READY" : "readiness: NOT READY") : supplied.Readiness;
            var platform = damage == null ? supplied.Platform : new StatusFact(
                damage.NewHpPct <= 0 ? StatusKnowledge.Offline
                    : damage.DamageLevel > 0 || damage.NewHpPct < 100 ? StatusKnowledge.Degraded : StatusKnowledge.Nominal,
                $"platform: HP {damage.NewHpPct:0.#}% / damage level {damage.DamageLevel}", damage.SequenceId, damage.SimTime);
            var commsFact = comms == null ? supplied.Comms : new StatusFact(
                comms.NewState == CommsState.Denied ? StatusKnowledge.Offline : comms.NewState == CommsState.Degraded ? StatusKnowledge.Degraded : StatusKnowledge.Nominal,
                $"comms: {comms.NewState.ToString().ToUpperInvariant()} ({comms.Reason})", comms.SequenceId, comms.SimTime);
            unitRows.Add(new StatusUnitRow(id, platform, supplied.Sensors, supplied.Mounts, commsFact,
                supplied.Mobility, readiness, supplied.Recovery));

            if (snapshot is IStatusSensorSource sensorSource && sensorSource.TryGetSensorStatus(id, out var sensor))
            {
                if (!string.Equals(sensor.UnitId, id, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Sensor source returned '{sensor.UnitId}' for requested unit '{id}'.");
                if (sensor.SourceSimTime is { } sensorTime
                    && (double.IsNaN(sensorTime) || double.IsInfinity(sensorTime) || sensorTime < 0 || sensorTime > snapshot.SimTime))
                    throw new InvalidOperationException($"Sensor facts for '{id}' have invalid or future source time.");
                var emitters = sensor.ActiveEmitterIds.OrderBy(x => x, StringComparer.Ordinal).ToArray();
                emissionRows.Add(new(id, StatusKnowledge.Nominal, sensor.EmconLevel, emitters,
                    emitters.Length == 0 ? $"EMCON {sensor.EmconLevel}: no active emissions" : $"EMCON {sensor.EmconLevel}: {string.Join(", ", emitters)} RADIATING"));
            }
            else
            {
                var input = EmconPostureProjection.BuildInput(id, bridge.Orchestrator.ScenarioPolicy,
                    CurrentCommsState(log, id, snapshot.SimTime));
                var posture = EmconPostureProjection.Project(input);
                emissionRows.Add(new(id, StatusKnowledge.Unknown, posture.EmconLevel,
                    Array.Empty<string>(), $"EMISSIONS: UNKNOWN; policy {posture.StatusLine}"));
            }

            if (snapshot is IStatusElectronicWarfareSource ewSource && ewSource.TryGetElectronicWarfareStatus(id, out var ew))
            {
                ValidateFactTime(id, "EW", ew, snapshot.SimTime);
                ewRows.Add(new(id, ew.State, ew.Detail));
            }
            else ewRows.Add(new(id, StatusKnowledge.Unknown, "EW: UNKNOWN"));
        }

        var platformDegradation = unitRows.All(HasCompleteComponentKnowledge)
            ? PlatformDegradeProjection.Project(new PlatformDegradeInput((ulong)snapshot.SimTime,
                unitRows.Select(ToPlatformDegradeInput).ToArray()))
            : null;
        var correlations = BuildCorrelations(log, snapshot.SimTime);
        return new StatusFrame(snapshot.SimTime, Array.AsReadOnly(contactRows), Array.AsReadOnly(unitRows.ToArray()),
            Array.AsReadOnly(emissionRows.ToArray()), Array.AsReadOnly(ewRows.ToArray()), platformDegradation,
            Array.AsReadOnly(correlations));
    }

    private static StatusUnitFacts UnknownUnit(string id) => new(id, StatusFact.Unknown("platform"),
        StatusFact.Unknown("sensors"), StatusFact.Unknown("mounts"), StatusFact.Unknown("comms"),
        StatusFact.Unknown("mobility"), StatusFact.Unknown("readiness"), StatusFact.Unknown("recovery"));

    private static StatusUnitFacts ValidateUnitFacts(string requestedId, StatusUnitFacts facts, double snapshotTime)
    {
        if (!string.Equals(facts.UnitId, requestedId, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unit source returned '{facts.UnitId}' for requested unit '{requestedId}'.");
        ValidateFactTime(requestedId, "platform", facts.Platform, snapshotTime);
        ValidateFactTime(requestedId, "sensors", facts.Sensors, snapshotTime);
        ValidateFactTime(requestedId, "mounts", facts.Mounts, snapshotTime);
        ValidateFactTime(requestedId, "comms", facts.Comms, snapshotTime);
        ValidateFactTime(requestedId, "mobility", facts.Mobility, snapshotTime);
        ValidateFactTime(requestedId, "readiness", facts.Readiness, snapshotTime);
        ValidateFactTime(requestedId, "recovery", facts.Recovery, snapshotTime);
        return facts;
    }

    private static void ValidateFactTime(string unitId, string subject, StatusFact fact, double snapshotTime)
    {
        if (fact.SourceSimTime is { } time && (double.IsNaN(time) || double.IsInfinity(time) || time < 0 || time > snapshotTime))
            throw new InvalidOperationException($"{subject} fact for '{unitId}' has invalid or future source time.");
    }

    private static bool HasCompleteComponentKnowledge(StatusUnitRow unit) =>
        unit.Sensors.State != StatusKnowledge.Unknown && unit.Mounts.State != StatusKnowledge.Unknown
        && unit.Comms.State != StatusKnowledge.Unknown && unit.Mobility.State != StatusKnowledge.Unknown;

    private static CommsState CurrentCommsState(DecisionLog log, string unitId, double simTime) => log.CommsStateChanges
        .Where(x => x.NodeId == unitId && x.SimTime <= simTime)
        .OrderBy(x => x.SequenceId).LastOrDefault()?.NewState ?? CommsState.Nominal;

    private static PlatformDegradeUnitInput ToPlatformDegradeInput(StatusUnitRow unit) => new(
        unit.UnitId,
        MobilityDegraded: IsDegraded(unit.Mobility), MobilitySeverity: Severity(unit.Mobility),
        SensorDegraded: IsDegraded(unit.Sensors), SensorSeverity: Severity(unit.Sensors),
        WeaponDegraded: IsDegraded(unit.Mounts), WeaponSeverity: Severity(unit.Mounts),
        CommsDegraded: IsDegraded(unit.Comms), CommsSeverity: Severity(unit.Comms));

    private static bool IsDegraded(StatusFact fact) => fact.State is StatusKnowledge.Degraded or StatusKnowledge.Offline;
    private static PlatformDegradeSeverityBand Severity(StatusFact fact) => fact.State == StatusKnowledge.Offline
        ? PlatformDegradeSeverityBand.Heavy : fact.State == StatusKnowledge.Degraded
            ? PlatformDegradeSeverityBand.Light : PlatformDegradeSeverityBand.None;

    private static StatusCorrelationRow[] BuildCorrelations(DecisionLog log, double simTime)
    {
        var result = new List<StatusCorrelationRow>();
        foreach (var entry in log.ChronologicalEntries())
        {
            if (entry.SimTime > simTime) continue;
            if (entry.Payload is PlatformDamageChangeRecord damage)
                result.Add(new(entry.SequenceId, entry.SimTime, damage.UnitId.Value, "DAMAGE",
                    $"HP {damage.PreviousHpPct:0.#}% -> {damage.NewHpPct:0.#}% / L{damage.DamageLevel}"));
            else if (entry.Payload is CommsStateChangeRecord comms)
                result.Add(new(entry.SequenceId, entry.SimTime, comms.NodeId, "COMMS",
                    $"{comms.PreviousState} -> {comms.NewState}: {comms.Reason}"));
        }
        return result.OrderBy(x => x.SequenceId).ToArray();
    }
}

public enum StatusDisplayFilter { All = 0, ProblemsOnly = 1 }

/// <summary>Display-only rows with text labels redundant to any future color treatment.</summary>
public sealed record StatusTextRow(string Id, string Text);
public sealed record StatusPresentation(IReadOnlyList<StatusTextRow> UnitRows, IReadOnlyList<StatusTextRow> ContactRows,
    IReadOnlyList<StatusTextRow> EmissionRows, IReadOnlyList<StatusTextRow> ElectronicWarfareRows,
    IReadOnlyList<StatusTextRow> TimelineRows);

public static class StatusPresenter
{
    public static StatusPresentation Build(StatusFrame frame, StatusDisplayFilter filter = StatusDisplayFilter.All)
    {
        if (frame is null) throw new ArgumentNullException(nameof(frame));
        var units = frame.Units.Where(u => filter == StatusDisplayFilter.All || HasProblem(u)).Select(u =>
            new StatusTextRow(u.UnitId, $"{u.UnitId}: PLATFORM {Label(u.Platform)} | SENSORS {Label(u.Sensors)} | MOUNTS {Label(u.Mounts)} | COMMS {Label(u.Comms)} | MOBILITY {Label(u.Mobility)} | READINESS {Label(u.Readiness)} | RECOVERY {Label(u.Recovery)}")).ToArray();
        var contacts = frame.Contacts.Select(c => new StatusTextRow(c.ContactId,
            $"{c.ContactId}: {c.Confidence.ToString().ToUpperInvariant()} / {c.Freshness.ToString().ToUpperInvariant()} / SOURCE {c.SourceRef} / AGE {c.AgeTicks} TICKS{(c.IsOutOfComms ? " / COMMS UNKNOWN" : string.Empty)}")).ToArray();
        var emissions = frame.Emissions.Select(e => new StatusTextRow(e.UnitId, $"{e.UnitId}: {e.Detail}")).ToArray();
        var ew = frame.ElectronicWarfare.Select(e => new StatusTextRow(e.UnitId, $"{e.UnitId}: {e.Detail}")).ToArray();
        var timeline = frame.Correlations.Select(c => new StatusTextRow(c.SequenceId.ToString(), $"#{c.SequenceId} T+{c.SimTime:0.###} {c.UnitId} {c.Kind}: {c.Detail}")).ToArray();
        return new(Array.AsReadOnly(units), Array.AsReadOnly(contacts), Array.AsReadOnly(emissions), Array.AsReadOnly(ew), Array.AsReadOnly(timeline));
    }

    private static bool HasProblem(StatusUnitRow u) => new[] { u.Platform, u.Sensors, u.Mounts, u.Comms, u.Mobility, u.Readiness }.Any(f => f.State is StatusKnowledge.Degraded or StatusKnowledge.Offline);
    private static string Label(StatusFact fact) => fact.State.ToString().ToUpperInvariant();
}
