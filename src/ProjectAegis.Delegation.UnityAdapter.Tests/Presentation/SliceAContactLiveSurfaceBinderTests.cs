using NUnit.Framework;
using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class SliceAContactLiveSurfaceBinderTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("missing")]
    public void Unknown_selection_fails_closed(string? contactId)
    {
        Assert.That(
            SliceAContactLiveSurfaceBinder.Bind(contactId, Frame(), CombatFrame()),
            Is.EqualTo(SliceAContactLiveSurfaceState.Empty));
    }

    [Test]
    public void Provenance_exposes_source_confidence_age_last_known_and_comms_without_color_only_state()
    {
        var result = SliceAContactLiveSurfaceBinder.Bind("c1", Frame(denied: true), CombatFrame());
        Assert.That(result.SourceLine, Does.Contain("sensor-1").And.Contain("source-ref"));
        Assert.That(result.ConfidenceLine, Does.Contain("HIGH"));
        Assert.That(result.AgeLine, Does.Contain("7 ticks").And.Contain("FRESH"));
        Assert.That(result.LastKnownLine, Does.Contain("Identified").And.Contain("target-1"));
        Assert.That(result.CommsLine, Does.Contain("DENIED"));
        Assert.That(result.DeclutterToken, Is.EqualTo(ContactProvenanceDeclutterTokens.CommsDenied));
        Assert.That(result.CommsCueClass, Is.EqualTo(ContactProvenanceCueClasses.Denied));
        Assert.That(result.ContactExplanationAvailable, Is.True);
    }

    [Test]
    public void Missing_provenance_fails_closed_on_live_surface_rows()
    {
        var frame = Frame() with
        {
            Provenance = ContactProvenanceSnapshot.Empty,
            KillChain = new KillChainContactSnapshot(
                new[] { new KillChainContactState("c1", "target-1", "sensor-1", KillChainPhase.Target,
                    KillChainLossKind.None, true, true, true, true, 1, 1, 2, 2, 2,
                    new ulong[] { 1 }, new[] { "source-ref" }) },
                Array.Empty<KillChainContactTransition>()),
        };
        var result = SliceAContactLiveSurfaceBinder.Bind("c1", frame, CombatFrame());
        Assert.That(result.SourceLine, Does.Contain("UNKNOWN"));
        Assert.That(result.ConfidenceLine, Does.Contain("UNKNOWN"));
        Assert.That(result.AgeLine, Does.Contain("UNKNOWN"));
        Assert.That(result.LastKnownLine, Does.Contain("UNKNOWN"));
        Assert.That(result.CommsLine, Does.Contain("UNKNOWN"));
        Assert.That(result.DeclutterToken, Is.EqualTo(ContactProvenanceDeclutterTokens.Unknown));
    }

    [Test]
    public void Stale_provenance_uses_declutter_token_and_degraded_age_cue()
    {
        var result = SliceAContactLiveSurfaceBinder.Bind("c1", Frame(stale: true), CombatFrame());
        Assert.That(result.AgeLine, Does.Contain("STALE"));
        Assert.That(result.DeclutterToken, Is.EqualTo(ContactProvenanceDeclutterTokens.Stale));
        Assert.That(result.AgeCueClass, Is.EqualTo(ContactProvenanceCueClasses.Degraded));
    }

    [Test]
    public void Engagement_deep_link_targets_latest_correlated_event_only()
    {
        var combat = CombatFrame(
            new CombatEvent(CombatEventPhase.Firing, "shooter-1", "target-1", "missile", "ok", 9, 9, 9, "engage-assess:launch"),
            new CombatEvent(CombatEventPhase.AuthorizationRefused, "shooter-2", "target-1", "gun", "denied", 10, 10, 10, "policy:denied"));
        var result = SliceAContactLiveSurfaceBinder.Bind("c1", Frame(), combat);
        Assert.That(result.EngagementExplanationAvailable, Is.True);
        Assert.That(result.EngagementInspectionKey, Is.EqualTo(
            CombatMapPresenter.KeyFor("shooter-2", "target-1", 10)));
    }

    [Test]
    public void Engagement_deep_link_unavailable_without_combat_events()
    {
        var result = SliceAContactLiveSurfaceBinder.Bind("c1", Frame(), CombatPresentationFrame.Empty);
        Assert.That(result.EngagementExplanationAvailable, Is.False);
        Assert.That(result.EngagementInspectionKey, Is.Null);
    }

    [Test]
    public void Panel_binder_maps_source_confidence_and_last_known_cue_classes()
    {
        var state = SliceAContactLiveSurfaceBinder.Bind("c1", Frame(denied: true), CombatFrame());
        var rows = SliceAContactLiveSurfacePanelBinder.BindRows(state);
        Assert.That(rows.Select(row => row.ElementName).ToArray(), Is.EqualTo(new[]
        {
            "source-line", "confidence-line", "age-line", "last-known-line", "comms-line",
        }));
        Assert.That(rows[0].CueClass, Is.EqualTo(ContactProvenanceCueClasses.Nominal));
        Assert.That(rows[1].CueClass, Is.EqualTo(ContactProvenanceCueClasses.Nominal));
        Assert.That(rows[3].CueClass, Is.EqualTo(ContactProvenanceCueClasses.Nominal));
        Assert.That(rows[4].CueClass, Is.EqualTo(ContactProvenanceCueClasses.Denied));
    }

    [Test]
    public void Repeated_bind_is_replay_stable()
    {
        var frame = Frame();
        var combat = CombatFrame();
        Assert.That(
            SliceAContactLiveSurfaceBinder.Bind("c1", frame, combat),
            Is.EqualTo(SliceAContactLiveSurfaceBinder.Bind("c1", frame, combat)));
    }

    private static SliceAContactFrame Frame(bool stale = false, bool denied = false) =>
        SliceAContactFrame.Empty with
        {
            SimTick = 9,
            SimTime = 9,
            Contacts = [new ContactPictureEntry("c1", "target-1", "sensor-1", "Tracked", 2, 2)],
            KillChain = new KillChainContactSnapshot(
                new[] { new KillChainContactState("c1", "target-1", "sensor-1", KillChainPhase.Target,
                    KillChainLossKind.None, true, true, true, true, 1, 1, 2, 2, 2,
                    new ulong[] { 1 }, new[] { "source-ref" }) },
                Array.Empty<KillChainContactTransition>()),
            Provenance = new ContactProvenanceSnapshot(
                new[] { new ContactProvenanceState("c1", new("sensor-1", "target-1", "source-ref"),
                    ContactProvenanceConfidence.High,
                    stale ? ContactProvenanceFreshness.Stale : ContactProvenanceFreshness.Fresh,
                    7,
                    new("Identified", "target-1", 2, 2),
                    denied,
                    denied ? ContactProvenanceQualityState.SilentComms : ContactProvenanceQualityState.None) }),
            Chains = SensorToShooterSnapshot.Empty,
        };

    private static CombatPresentationFrame CombatFrame(params CombatEvent[] events) =>
        new(new CombatEventSnapshot(events), SliceAContactFrame.Empty, BdaAssessSnapshot.Empty, 0);
}
