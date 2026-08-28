using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.EmconPosture;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.EmconPosture;

public sealed class EmconPostureProjectionTests
{
    private static EmconPostureInput ActiveInput(string unitId = "u1") =>
        new(unitId, EmconState.Active);

    [Test]
    public void Active_radiating_lists_sensors_without_silent_cause()
    {
        var posture = EmconPostureProjection.Project(ActiveInput());

        Assert.That(posture.EmconLevel, Is.EqualTo(EmconState.Active));
        Assert.That(posture.PostureKind, Is.EqualTo(EmconPostureKind.AdvisoryEmissionsPosture));
        Assert.That(posture.IsAdvisoryOnly, Is.True);
        Assert.That(posture.IsEmitterToggleAuthorization, Is.False);
        Assert.That(posture.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(posture.IsFireOrder, Is.False);
        Assert.That(posture.SilentCause, Is.EqualTo(EmconPostureSilentCause.None));
        Assert.That(posture.SilentCauseCode, Is.Null);
        Assert.That(posture.SilentCauseLabel, Is.Empty);
        Assert.That(posture.RadiatingSensors, Has.Count.EqualTo(1));
        Assert.That(posture.RadiatingSensors[0].EmitterId, Is.EqualTo(CatalogRadarEmconResolver.DefaultEmitterId));
        Assert.That(posture.Assumptions, Does.Contain("Advisory emissions posture only — not emitter toggle authorization."));
        Assert.That(posture.Assumptions, Does.Contain("Policy EMCON level is ACTIVE."));
        Assert.That(posture.StatusLine, Does.Contain("EMCON: ACTIVE"));
        Assert.That(posture.StatusLine, Does.Contain("radiating:"));
        Assert.That(posture.StatusLine, Does.Contain("not emitter toggle"));
    }

    [Test]
    public void Off_silent_reports_emcon_off_with_no_radiating_sensors_and_advisory_flags_false()
    {
        var input = new EmconPostureInput("u1", EmconState.Off);
        var posture = EmconPostureProjection.Project(input);

        Assert.That(posture.EmconLevel, Is.EqualTo(EmconState.Off));
        Assert.That(posture.RadiatingSensors, Is.Empty);
        Assert.That(posture.SilentCause, Is.EqualTo(EmconPostureSilentCause.PolicyOff));
        Assert.That(posture.SilentCauseCode, Is.EqualTo(AbortReasonCatalog.Doctrine.EMCON_OFF));
        Assert.That(posture.SilentCauseLabel, Is.EqualTo(EmconPostureSilentCauseLabels.PolicyOff));
        Assert.That(posture.IsEmitterToggleAuthorization, Is.False);
        Assert.That(posture.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(posture.IsFireOrder, Is.False);
        Assert.That(posture.Assumptions, Does.Contain("Not fully radiating — policy EMCON off."));
        Assert.That(posture.StatusLine, Does.Contain("silent: EMCON_OFF"));
    }

    [Test]
    public void Passive_silent_reports_standby_cause_with_no_radiating_sensors()
    {
        var input = new EmconPostureInput("u1", EmconState.Passive);
        var posture = EmconPostureProjection.Project(input);

        Assert.That(posture.EmconLevel, Is.EqualTo(EmconState.Passive));
        Assert.That(posture.RadiatingSensors, Is.Empty);
        Assert.That(posture.SilentCause, Is.EqualTo(EmconPostureSilentCause.StandbyPassive));
        Assert.That(posture.SilentCauseCode, Is.Null);
        Assert.That(posture.SilentCauseLabel, Is.EqualTo(EmconPostureSilentCauseLabels.StandbyPassive));
        Assert.That(posture.IsEmitterToggleAuthorization, Is.False);
        Assert.That(posture.StatusLine, Does.Contain("silent: passive / standby"));
    }

    [Test]
    public void Denied_silent_reports_comms_denied_with_no_radiating_sensors_and_advisory_flags_false()
    {
        var input = new EmconPostureInput("u1", EmconState.Active, CommsState.Denied);
        var posture = EmconPostureProjection.Project(input);

        Assert.That(posture.EmconLevel, Is.EqualTo(EmconState.Active));
        Assert.That(posture.RadiatingSensors, Is.Empty);
        Assert.That(posture.SilentCause, Is.EqualTo(EmconPostureSilentCause.CommsDenied));
        Assert.That(posture.SilentCauseCode, Is.EqualTo(AbortReasonCatalog.Doctrine.COMMS_DENIED));
        Assert.That(posture.IsEmitterToggleAuthorization, Is.False);
        Assert.That(posture.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(posture.IsFireOrder, Is.False);
        Assert.That(posture.Assumptions, Does.Contain("Comms state is denied."));
        Assert.That(posture.StatusLine, Does.Contain("silent: COMMS_DENIED"));
    }

    [Test]
    public void Identical_inputs_yield_identical_fingerprint()
    {
        var input = ActiveInput("2001");
        var a = EmconPostureProjection.Project(input);
        var b = EmconPostureProjection.Project(input);

        Assert.That(
            EmconPostureProjection.ComputeFingerprint(a),
            Is.EqualTo(EmconPostureProjection.ComputeFingerprint(b)));
    }

    [Test]
    public void Empty_unit_id_returns_empty_posture_fingerprint()
    {
        var posture = EmconPostureProjection.Project(new EmconPostureInput(string.Empty, EmconState.Active));

        Assert.That(posture.UnitId, Is.Empty);
        Assert.That(EmconPostureProjection.ComputeFingerprint(posture), Is.EqualTo("emcon:empty"));
    }

    [Test]
    public void Multi_emitter_input_lists_only_active_radiators_when_policy_active()
    {
        var input = new EmconPostureInput(
            "u1",
            EmconState.Active,
            Emitters:
            [
                new EmconEmitterFact("radar-1", EmconState.Active),
                new EmconEmitterFact("esm-1", EmconState.Passive),
                new EmconEmitterFact("jammer-1", EmconState.Off),
            ]);
        var posture = EmconPostureProjection.Project(input);

        Assert.That(posture.RadiatingSensors, Has.Count.EqualTo(1));
        Assert.That(posture.RadiatingSensors[0].EmitterId, Is.EqualTo("radar-1"));
        Assert.That(posture.SilentCause, Is.EqualTo(EmconPostureSilentCause.None));
    }

    [Test]
    public void Active_with_explicit_empty_emitters_stays_empty_without_default_radar_fallback()
    {
        var input = new EmconPostureInput(
            "u1",
            EmconState.Active,
            Emitters: Array.Empty<EmconEmitterFact>());
        var posture = EmconPostureProjection.Project(input);

        Assert.That(posture.EmconLevel, Is.EqualTo(EmconState.Active));
        Assert.That(posture.RadiatingSensors, Is.Empty);
        Assert.That(posture.SilentCause, Is.EqualTo(EmconPostureSilentCause.None));
        Assert.That(posture.SilentCauseCode, Is.Null);
        Assert.That(posture.Assumptions, Does.Contain("No emitters are actively radiating."));
        Assert.That(posture.StatusLine, Does.Contain("radiating: none"));
    }
}
