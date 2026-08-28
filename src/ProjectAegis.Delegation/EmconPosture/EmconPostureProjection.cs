namespace ProjectAegis.Delegation.EmconPosture;

using System.Globalization;
using System.Text;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// DRG-221: headless EMCON/emissions posture projector.
/// Consumes policy EMCON and comms facts only — never toggles emitters or mutates sim state.
/// </summary>
public static class EmconPostureProjection
{
    private const string AdvisoryAssumption =
        "Advisory emissions posture only — not emitter toggle authorization.";

    /// <summary>Projects advisory emissions posture from read-only EMCON and comms facts.</summary>
    public static EmissionsPosture Project(in EmconPostureInput input)
    {
        if (string.IsNullOrWhiteSpace(input.UnitId))
        {
            return EmissionsPosture.Empty;
        }

        var emitters = ResolveEmitters(in input);
        var (silentCause, silentCauseCode) = ResolveSilentCause(input.EmconLevel, input.CommsState);
        var radiating = BuildRadiatingSensors(emitters, silentCause);
        var assumptions = BuildAssumptions(in input, silentCause, radiating);
        var statusLine = BuildStatusLine(input.EmconLevel, radiating, silentCause, silentCauseCode);

        return new EmissionsPosture(
            input.UnitId,
            input.EmconLevel,
            EmconPostureKind.AdvisoryEmissionsPosture,
            IsAdvisoryOnly: true,
            IsEmitterToggleAuthorization: false,
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false,
            radiating,
            assumptions,
            silentCause,
            silentCauseCode,
            EmconPostureSilentCauseLabels.Format(silentCause),
            statusLine);
    }

    /// <summary>Builds projection input from scenario policy EMCON overrides and optional catalog fallback.</summary>
    public static EmconPostureInput BuildInput(
        string unitId,
        ScenarioPolicyProfile? policy,
        CommsState commsState = CommsState.Nominal,
        ICatalogReader? catalog = null)
    {
        var emconLevel = ScenarioEmconResolver.ResolveRadar(
            unitId,
            policy?.UnitRadarEmcon,
            catalog);
        return new EmconPostureInput(unitId, emconLevel, commsState);
    }

    /// <summary>Replay-stable canonical form: same input yields the same string. Invariant culture; no wall clock.</summary>
    public static string ComputeFingerprint(EmissionsPosture? posture)
    {
        if (posture is null || string.IsNullOrWhiteSpace(posture.UnitId))
        {
            return "emcon:empty";
        }

        var builder = new StringBuilder();
        builder.Append("emcon:");
        builder.Append(posture.UnitId);
        builder.Append('|');
        builder.Append((int)posture.EmconLevel);
        builder.Append('|');
        builder.Append((int)posture.PostureKind);
        builder.Append('|');
        builder.Append(posture.IsAdvisoryOnly ? '1' : '0');
        builder.Append(posture.IsEmitterToggleAuthorization ? '1' : '0');
        builder.Append(posture.IsWeaponsReleaseAuthorization ? '1' : '0');
        builder.Append(posture.IsFireOrder ? '1' : '0');
        builder.Append('|');
        builder.Append((int)posture.SilentCause);
        builder.Append('|');
        builder.Append(posture.SilentCauseCode ?? string.Empty);
        builder.Append('|');
        builder.Append("s=");
        builder.Append(posture.RadiatingSensors.Count);
        for (var i = 0; i < posture.RadiatingSensors.Count; i++)
        {
            var sensor = posture.RadiatingSensors[i];
            builder.Append('|');
            builder.Append(sensor.EmitterId);
            builder.Append(',');
            builder.Append(sensor.Label);
        }

        builder.Append("|a=");
        builder.Append(posture.Assumptions.Count);
        for (var i = 0; i < posture.Assumptions.Count; i++)
        {
            builder.Append('|');
            builder.Append(posture.Assumptions[i]);
        }

        return builder.ToString();
    }

    private static IReadOnlyList<EmconEmitterFact> ResolveEmitters(in EmconPostureInput input)
    {
        // Explicit empty inventory is authoritative; only null falls back to default radar-1.
        if (input.Emitters is not null)
        {
            return input.Emitters
                .OrderBy(e => e.EmitterId, StringComparer.Ordinal)
                .ToArray();
        }

        return
        [
            new EmconEmitterFact(CatalogRadarEmconResolver.DefaultEmitterId, input.EmconLevel),
        ];
    }

    private static (EmconPostureSilentCause Cause, string? Code) ResolveSilentCause(
        EmconState emconLevel,
        CommsState commsState)
    {
        if (commsState == CommsState.Denied)
        {
            return (EmconPostureSilentCause.CommsDenied, AbortReasonCatalog.Doctrine.COMMS_DENIED);
        }

        return emconLevel switch
        {
            EmconState.Off => (EmconPostureSilentCause.PolicyOff, AbortReasonCatalog.Doctrine.EMCON_OFF),
            EmconState.Passive => (EmconPostureSilentCause.StandbyPassive, null),
            _ => (EmconPostureSilentCause.None, null),
        };
    }

    private static IReadOnlyList<RadiatingSensor> BuildRadiatingSensors(
        IReadOnlyList<EmconEmitterFact> emitters,
        EmconPostureSilentCause silentCause)
    {
        if (silentCause != EmconPostureSilentCause.None)
        {
            return Array.Empty<RadiatingSensor>();
        }

        var radiating = new List<RadiatingSensor>();
        foreach (var emitter in emitters)
        {
            if (emitter.State != EmconState.Active)
            {
                continue;
            }

            radiating.Add(new RadiatingSensor(emitter.EmitterId, FormatEmitterLabel(emitter.EmitterId)));
        }

        return radiating;
    }

    private static IReadOnlyList<string> BuildAssumptions(
        in EmconPostureInput input,
        EmconPostureSilentCause silentCause,
        IReadOnlyList<RadiatingSensor> radiatingSensors)
    {
        var assumptions = new List<string> { AdvisoryAssumption };
        assumptions.Add($"Policy EMCON level is {FormatEmconLevel(input.EmconLevel)}.");
        assumptions.Add("Emissions posture is distinct from C2 network health and agent Skills.");

        if (input.CommsState != CommsState.Nominal)
        {
            assumptions.Add($"Comms state is {input.CommsState.ToString().ToLowerInvariant()}.");
        }

        if (silentCause == EmconPostureSilentCause.None)
        {
            assumptions.Add(
                radiatingSensors.Count == 0
                    ? "No emitters are actively radiating."
                    : $"Radiating emitters: {string.Join(", ", radiatingSensors.Select(s => s.EmitterId))}.");
        }
        else
        {
            var label = EmconPostureSilentCauseLabels.Format(silentCause);
            assumptions.Add($"Not fully radiating — {label}.");
        }

        return assumptions;
    }

    private static string BuildStatusLine(
        EmconState emconLevel,
        IReadOnlyList<RadiatingSensor> radiatingSensors,
        EmconPostureSilentCause silentCause,
        string? silentCauseCode)
    {
        var level = FormatEmconLevel(emconLevel);
        if (silentCause == EmconPostureSilentCause.None)
        {
            var emitters = radiatingSensors.Count == 0
                ? "none"
                : string.Join(", ", radiatingSensors.Select(s => s.EmitterId));
            return FormattableString.Invariant(
                $"EMCON: {level} | radiating: {emitters} (advisory — not emitter toggle)");
        }

        var cause = silentCauseCode ?? EmconPostureSilentCauseLabels.Format(silentCause);
        return FormattableString.Invariant(
            $"EMCON: {level} | silent: {cause} (advisory — not emitter toggle)");
    }

    private static string FormatEmconLevel(EmconState state) =>
        state switch
        {
            EmconState.Active => "ACTIVE",
            EmconState.Passive => "PASSIVE",
            EmconState.Off => "OFF",
            _ => state.ToString().ToUpperInvariant(),
        };

    private static string FormatEmitterLabel(string emitterId) =>
        emitterId.Replace("-", " ");
}
