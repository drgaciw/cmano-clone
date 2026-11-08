using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.TargetabilityAccept;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class ContactCombatCardProjectionTests
{
    [Test]
    public void Missing_selection_is_explicit_unknown_contact_card()
    {
        var card = ContactCombatCardProjection.Project(null, null, null, null, null);

        AssertBlank(card);
        Assert.That(card, Is.SameAs(ContactCombatCard.Empty));
        Assert.That(ContactCombatCardProjection.Project("  ", Provenance(), Targetability(), Bda(), Ledger(), "defensive"),
            Is.SameAs(ContactCombatCard.Empty));
    }

    [Test]
    public void Missing_inputs_for_selected_contact_stay_unknown()
    {
        var card = ContactCombatCardProjection.Project("c1", null, null, null, null, postureLabel: null);

        Assert.That(card.HasContact, Is.True);
        Assert.That(card.ContactId, Is.EqualTo("c1"));
        Assert.That(card.SurfaceKind, Is.EqualTo(ContactCombatCardTokens.SurfaceKind));
        Assert.That(card.Provenance, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Freshness, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Confidence, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Posture, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Targetability, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.EngagementStatus, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Bda, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Fingerprint, Does.StartWith("ccc:surface=contact-combat;has=1;id=c1;"));
        Assert.That(ContactCombatCardProjection.FingerprintMatches(card), Is.True);
    }

    [Test]
    public void Known_rows_are_copied_without_filling_gaps()
    {
        var card = ContactCombatCardProjection.Project(
            "c1",
            Provenance(),
            Targetability(),
            Bda(),
            Ledger(),
            "defensive");

        Assert.That(card.Provenance, Is.EqualTo(
            "obs=sensor-1;tgt=target-1;ref=observer:sensor-1|target:target-1;life=Identified;knownTgt=target-1;quality=None;ooc=0;tick=12;time=1.5"));
        Assert.That(card.Freshness, Is.EqualTo("Fresh;age=4"));
        Assert.That(card.Confidence, Is.EqualTo("High"));
        Assert.That(card.Posture, Is.EqualTo("defensive"));
        Assert.That(card.Targetability, Is.EqualTo("Withheld;cause=Stale"));
        Assert.That(card.EngagementStatus, Is.EqualTo(
            "shooter=u1;phase=TerminalOutcome;outcome=Hit;weapon=asm;tick=20;time=2.5;corr=9;expl=ref-9"));
        Assert.That(card.Bda, Is.EqualTo(
            "state=Damaged;source=PlatformDamage;tgt=target-1;obs=sensor-1;tick=21;time=3.25;corr=3"));
        Assert.That(card.Provenance, Does.Not.Contain(UnitCommsDisplay.Operational));
        Assert.That(card.EngagementStatus, Does.Not.Contain(UnitCommsDisplay.Destroyed));
        Assert.That(ContactCombatCardProjection.FingerprintMatches(card), Is.True);
    }

    [Test]
    public void Recorded_confidence_unknown_is_distinct_from_missing_provenance()
    {
        var recorded = ContactCombatCardProjection.Project(
            "c1",
            Provenance(confidence: ContactProvenanceConfidence.Unknown),
            null,
            null,
            null);
        var missing = ContactCombatCardProjection.Project("c1", null, null, null, null);

        Assert.That(recorded.Confidence, Is.EqualTo("Unknown"));
        Assert.That(missing.Confidence, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(recorded.Confidence, Is.Not.EqualTo(missing.Confidence));
    }

    [Test]
    public void Bda_state_unknown_and_none_are_not_rewritten()
    {
        var unknown = ContactCombatCardProjection.Project(
            "c1", null, null, Bda(state: BdaAssessStateKind.Unknown, source: BdaAssessSourceKind.ContactLifecycle), null);
        var none = ContactCombatCardProjection.Project(
            "c1", null, null, Bda(state: BdaAssessStateKind.None, source: BdaAssessSourceKind.None), null);
        var missing = ContactCombatCardProjection.Project("c1", null, null, null, null);

        Assert.That(unknown.Bda, Does.StartWith("state=Unknown;source=ContactLifecycle;"));
        Assert.That(none.Bda, Does.StartWith("state=None;source=None;"));
        Assert.That(missing.Bda, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(none.Bda, Does.Not.Contain("Damaged"));
        Assert.That(none.Bda, Does.Not.Contain("Destroyed"));
    }

    [Test]
    public void Terminal_engagement_does_not_invent_bda()
    {
        var card = ContactCombatCardProjection.Project("c1", Provenance(), null, null, Ledger(outcome: "Kill"));

        Assert.That(card.EngagementStatus, Does.Contain("phase=TerminalOutcome").And.Contain("outcome=Kill"));
        Assert.That(card.Bda, Is.EqualTo(ContactCombatCardTokens.Unknown));
    }

    [Test]
    public void Bda_destroyed_does_not_invent_engagement()
    {
        var card = ContactCombatCardProjection.Project(
            "c1",
            null,
            null,
            Bda(state: BdaAssessStateKind.Destroyed, source: BdaAssessSourceKind.EngagementOutcome),
            null);

        Assert.That(card.Bda, Does.Contain("state=Destroyed"));
        Assert.That(card.EngagementStatus, Is.EqualTo(ContactCombatCardTokens.Unknown));
    }

    [Test]
    public void High_confidence_does_not_permit_targetability()
    {
        var card = ContactCombatCardProjection.Project("c1", Provenance(), Targetability(), null, null);

        Assert.That(card.Confidence, Is.EqualTo("High"));
        Assert.That(card.Targetability, Is.EqualTo("Withheld;cause=Stale"));
        Assert.That(card.Targetability, Does.Not.Contain("Permitted"));
    }

    [Test]
    public void Posture_is_not_inferred_when_omitted()
    {
        var card = ContactCombatCardProjection.Project("c1", Provenance(), Targetability(), Bda(), Ledger());

        Assert.That(card.Posture, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Posture, Is.Not.EqualTo("Offensive"));
        Assert.That(card.Posture, Is.Not.EqualTo("Defensive"));
    }

    [Test]
    public void Whitespace_posture_stays_unknown_and_supplied_label_is_verbatim()
    {
        var blank = ContactCombatCardProjection.Project("c1", null, null, null, null, "  ");
        var supplied = ContactCombatCardProjection.Project("c1", null, null, null, null, " defensive ");

        Assert.That(blank.Posture, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(supplied.Posture, Is.EqualTo(" defensive "));
    }

    [Test]
    public void Freshness_label_is_not_recomputed_from_age()
    {
        var card = ContactCombatCardProjection.Project(
            "c1",
            Provenance(freshness: ContactProvenanceFreshness.Fresh, age: 400),
            null,
            null,
            null);

        Assert.That(card.Freshness, Is.EqualTo("Fresh;age=400"));
        Assert.That(card.Freshness, Does.Not.Contain("Stale"));
    }

    [Test]
    public void Disagreeing_target_ids_do_not_select_an_engagement()
    {
        var provenance = Provenance(targetId: "target-1", knownTargetId: "target-other");
        var card = ContactCombatCardProjection.Project("c1", provenance, null, null, Ledger());

        Assert.That(card.Provenance, Does.Contain("tgt=target-1").And.Contain("knownTgt=target-other"));
        Assert.That(card.EngagementStatus, Is.EqualTo(ContactCombatCardTokens.Unknown));
    }

    [Test]
    public void Targetability_for_a_different_target_than_provenance_is_unknown()
    {
        var card = ContactCombatCardProjection.Project(
            "c1", Provenance(), Targetability(targetId: "target-2"), null, Ledger());

        Assert.That(card.Provenance, Does.Contain("tgt=target-1"));
        Assert.That(card.Targetability, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.EngagementStatus, Is.EqualTo(ContactCombatCardTokens.Unknown));
    }

    [Test]
    public void Targetability_mismatch_also_withholds_bda_that_disagrees_with_it()
    {
        var card = ContactCombatCardProjection.Project(
            "c1", Provenance(), Targetability(targetId: "target-2"), Bda(), Ledger());

        Assert.That(card.Targetability, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Bda, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.EngagementStatus, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(ContactCombatCardProjection.FingerprintMatches(card), Is.True);
    }

    [Test]
    public void Bda_for_a_different_target_than_provenance_is_unknown()
    {
        var card = ContactCombatCardProjection.Project(
            "c1", Provenance(), Targetability(), Bda(targetId: "target-2"), Ledger());

        Assert.That(card.Bda, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Bda, Does.Not.Contain("target-2"));
        Assert.That(card.Targetability, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.EngagementStatus, Is.EqualTo(ContactCombatCardTokens.Unknown));
    }

    [Test]
    public void Bda_for_a_different_target_than_provenance_last_known_is_unknown()
    {
        var card = ContactCombatCardProjection.Project(
            "c1", Provenance(targetId: "target-2", knownTargetId: "target-1"), null, Bda(targetId: "target-2"), null);

        Assert.That(card.Bda, Is.EqualTo(ContactCombatCardTokens.Unknown));
    }

    [Test]
    public void Targetability_and_bda_disagreeing_without_provenance_are_unknown()
    {
        var card = ContactCombatCardProjection.Project(
            "c1", null, Targetability(targetId: "target-1"), Bda(targetId: "target-2"), null);

        Assert.That(card.Targetability, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Bda, Is.EqualTo(ContactCombatCardTokens.Unknown));
    }

    [Test]
    public void Assessments_without_conflicting_identity_facts_are_read()
    {
        var card = ContactCombatCardProjection.Project(
            "c1", null, Targetability(targetId: "target-2"), Bda(targetId: "target-2"), null);

        Assert.That(card.Targetability, Is.EqualTo("Withheld;cause=Stale"));
        Assert.That(card.Bda, Does.Contain("tgt=target-2"));
    }

    [Test]
    public void Ambiguous_duplicate_provenance_is_unknown()
    {
        var snapshot = new ContactProvenanceSnapshot(new[]
        {
            ProvenanceRow(),
            ProvenanceRow(confidence: ContactProvenanceConfidence.Low),
        });

        var card = ContactCombatCardProjection.Project("c1", snapshot, null, null, null);

        Assert.That(card.Provenance, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Freshness, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Confidence, Is.EqualTo(ContactCombatCardTokens.Unknown));
    }

    [Test]
    public void Identical_duplicate_provenance_is_read()
    {
        var row = ProvenanceRow();
        var snapshot = new ContactProvenanceSnapshot(new[] { row, row });

        var card = ContactCombatCardProjection.Project("c1", snapshot, null, null, null);

        Assert.That(card.Confidence, Is.EqualTo("High"));
        Assert.That(card.Provenance, Does.Contain("obs=sensor-1"));
    }

    [Test]
    public void Latest_engagement_is_the_last_matching_ledger_entry()
    {
        var older = Entry(tick: 10, outcome: "Launched", phase: CombatEventPhaseConsume.Firing);
        var newer = Entry(tick: 20, outcome: "Hit", phase: CombatEventPhaseConsume.TerminalOutcome);
        var other = Entry(targetId: "target-other", tick: 30, outcome: "Miss");

        var card = ContactCombatCardProjection.Project(
            "c1", Provenance(), null, null, new AfterActionLedgerSnapshot(new[] { older, newer, other }));

        Assert.That(card.EngagementStatus, Does.Contain("outcome=Hit").And.Contain("tick=20"));
        Assert.That(ContactCombatCardProjection.FingerprintMatches(card), Is.True);
    }

    [Test]
    public void Same_tick_attempts_use_ledger_order_not_correlation_id()
    {
        var secondAttemptFiring = AttemptEntry("u2", 10, CombatEventPhaseConsume.Firing, "Launched", "ref-10");
        var firstAttemptTerminal = AttemptEntry("u1", 4, CombatEventPhaseConsume.TerminalOutcome, "Kill", "ref-4");

        var card = ContactCombatCardProjection.Project(
            "c1",
            Provenance(),
            null,
            null,
            new AfterActionLedgerSnapshot(new[] { secondAttemptFiring, firstAttemptTerminal }));

        Assert.That(card.EngagementStatus, Is.EqualTo(
            "shooter=u1;phase=TerminalOutcome;outcome=Kill;weapon=asm;tick=20;time=2.5;corr=4;expl=ref-4"));
    }

    [Test]
    public void Same_tick_terminal_outcomes_use_ledger_order_not_field_tie_break()
    {
        var earlierTerminal = AttemptEntry("u2", 9, CombatEventPhaseConsume.TerminalOutcome, "Miss", "ref-a");
        var laterTerminal = AttemptEntry("u1", 9, CombatEventPhaseConsume.TerminalOutcome, "Hit", "ref-b");
        var laterFiring = AttemptEntry("u1", 9, CombatEventPhaseConsume.Firing, "Launched", "ref-c");

        var terminalLast = ContactCombatCardProjection.Project(
            "c1", Provenance(), null, null, new AfterActionLedgerSnapshot(new[] { earlierTerminal, laterTerminal }));
        var firingLast = ContactCombatCardProjection.Project(
            "c1", Provenance(), null, null, new AfterActionLedgerSnapshot(new[] { earlierTerminal, laterFiring }));

        Assert.That(terminalLast.EngagementStatus, Does.Contain("shooter=u1").And.Contain("outcome=Hit").And.Contain("expl=ref-b"));
        Assert.That(firingLast.EngagementStatus, Does.Contain("phase=Firing").And.Contain("expl=ref-c"));
    }

    [Test]
    public void Other_contact_bda_and_contact_id_as_target_are_not_used()
    {
        var bda = new BdaAssessSnapshot(new[]
        {
            BdaRow(contactId: "c-other", targetId: "c1"),
        });
        var ledger = new AfterActionLedgerSnapshot(new[]
        {
            Entry(targetId: "c1"),
        });

        var card = ContactCombatCardProjection.Project("c1", null, null, bda, ledger);

        Assert.That(card.Bda, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.EngagementStatus, Is.EqualTo(ContactCombatCardTokens.Unknown));
    }

    [Test]
    public void Snapshot_orders_contact_ids_ordinally_and_is_replay_stable()
    {
        var ids = new[] { "contact-2", "contact-10", "contact-2", "  ", "contact-1" };
        var first = ContactCombatCardProjection.ProjectSelection(ids, Provenance(), null, null, null, Postures());
        var second = ContactCombatCardProjection.ProjectSelection(
            new[] { "contact-1", "contact-10", "contact-2" },
            Provenance(),
            null,
            null,
            null,
            Postures());

        Assert.That(first.Cards.Select(card => card.ContactId), Is.EqualTo(new[] { "contact-1", "contact-10", "contact-2" }));
        Assert.That(first.Cards[0].Posture, Is.EqualTo("screen"));
        Assert.That(first.Cards[1].Posture, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(first.Cards[2].Posture, Is.EqualTo("raid"));
        Assert.That(first.Fingerprint, Does.StartWith("ccc:n=3|"));
        Assert.That(first.Fingerprint, Is.EqualTo(second.Fingerprint));
        Assert.That(ContactCombatCardProjection.FingerprintMatches(first), Is.True);
        Assert.That(first.Cards[0].Fingerprint, Is.Not.EqualTo(ContactCombatCardTokens.EmptyFingerprint));
    }

    [Test]
    public void Snapshot_cards_cannot_be_cast_back_to_a_mutable_array()
    {
        var snapshot = ContactCombatCardProjection.ProjectSelection(
            new[] { "c1", "c2" }, Provenance(), null, null, null);
        var original = snapshot.Cards[0];

        Assert.That(snapshot.Cards, Is.Not.InstanceOf<ContactCombatCard[]>());
        Assert.That(snapshot.Cards, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<ContactCombatCard>>());
        var asList = (IList<ContactCombatCard>)snapshot.Cards;
        Assert.That(asList.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => asList[0] = ContactCombatCard.Empty);
        Assert.That(snapshot.Cards[0], Is.SameAs(original));
        Assert.That(ContactCombatCardProjection.FingerprintMatches(snapshot), Is.True);
    }

    [Test]
    public void Snapshot_copies_caller_array_so_later_writes_do_not_desync_fingerprint()
    {
        var card = ContactCombatCardProjection.Project("c1", Provenance(), null, null, null);
        var source = new[] { card };
        var snapshot = new ContactCombatCardSnapshot(source, ContactCombatCardProjection.ComputeFingerprint(source));

        source[0] = ContactCombatCard.Empty;

        Assert.That(snapshot.Cards[0], Is.SameAs(card));
        Assert.That(ContactCombatCardProjection.FingerprintMatches(snapshot), Is.True);
    }

    [Test]
    public void Conflicting_posture_facts_are_unknown()
    {
        var postures = new[]
        {
            new ContactCombatCardPostureFact("c1", "screen"),
            new ContactCombatCardPostureFact("c1", "raid"),
        };

        var snapshot = ContactCombatCardProjection.ProjectSelection(
            new[] { "c1" },
            null,
            null,
            null,
            null,
            postures);

        Assert.That(snapshot.Cards[0].Posture, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(snapshot.Cards[0].ContactId, Is.EqualTo("c1"));
    }

    [Test]
    public void Empty_contact_list_is_empty_snapshot()
    {
        var snapshot = ContactCombatCardProjection.ProjectSelection(
            new[] { " ", "" },
            Provenance(),
            Targetability(),
            Bda(),
            Ledger(),
            Postures());

        Assert.That(snapshot.Cards, Is.Empty);
        Assert.That(snapshot.Fingerprint, Is.EqualTo(ContactCombatCardTokens.EmptyFingerprint));
        Assert.That(
            ContactCombatCardProjection.ProjectSelection(null, null, null, null, null),
            Is.SameAs(ContactCombatCardSnapshot.Empty));
        Assert.That(ContactCombatCardProjection.FingerprintMatches(ContactCombatCardSnapshot.Empty), Is.True);
    }

    [Test]
    public void Quality_flags_are_copied()
    {
        var card = ContactCombatCardProjection.Project(
            "c1",
            Provenance(quality: ContactProvenanceQualityState.CatalogMiss | ContactProvenanceQualityState.Stale, outOfComms: true),
            null,
            null,
            null);

        Assert.That(card.Provenance, Does.Contain("quality=CatalogMiss, Stale").And.Contain("ooc=1"));
    }

    [Test]
    public void Provenance_list_order_does_not_change_the_card()
    {
        var low = ProvenanceRow(contactId: "c-low", confidence: ContactProvenanceConfidence.Low);
        var high = ProvenanceRow();
        var forward = new ContactProvenanceSnapshot(new[] { low, high });
        var reversed = new ContactProvenanceSnapshot(new[] { high, low });

        var a = ContactCombatCardProjection.Project("c1", forward, null, null, null);
        var b = ContactCombatCardProjection.Project("c1", reversed, null, null, null);

        Assert.That(a.Fingerprint, Is.EqualTo(b.Fingerprint));
        Assert.That(forward.Contacts[0].ContactId, Is.EqualTo("c-low"));
        Assert.That(reversed.Contacts[0].ContactId, Is.EqualTo("c1"));
    }

    [Test]
    public void Card_shape_is_distinct_from_own_unit_status()
    {
        var card = ContactCombatCardProjection.Project("c1", Provenance(), Targetability(), Bda(), Ledger(), "defensive");
        var properties = typeof(ContactCombatCard).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToArray();

        Assert.That(card.SurfaceKind, Is.EqualTo("contact-combat"));
        Assert.That(properties, Does.Contain("Provenance"));
        Assert.That(properties, Does.Contain("Bda"));
        Assert.That(properties, Does.Not.Contain("IsAlive"));
        Assert.That(properties, Does.Not.Contain("CommsState"));
        Assert.That(properties, Is.Not.EqualTo(typeof(UnitCommsDisplay).GetProperties().Select(property => property.Name)));
    }

    [Test]
    public void Repeated_projection_is_replay_stable()
    {
        var first = ContactCombatCardProjection.Project("c1", Provenance(), Targetability(), Bda(), Ledger(), "defensive");
        var second = ContactCombatCardProjection.Project("c1", Provenance(), Targetability(), Bda(), Ledger(), "defensive");

        Assert.That(second.ContactId, Is.EqualTo(first.ContactId));
        Assert.That(second.Provenance, Is.EqualTo(first.Provenance));
        Assert.That(second.Freshness, Is.EqualTo(first.Freshness));
        Assert.That(second.Confidence, Is.EqualTo(first.Confidence));
        Assert.That(second.Posture, Is.EqualTo(first.Posture));
        Assert.That(second.Targetability, Is.EqualTo(first.Targetability));
        Assert.That(second.EngagementStatus, Is.EqualTo(first.EngagementStatus));
        Assert.That(second.Bda, Is.EqualTo(first.Bda));
        Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
        Assert.That(second.HasContact, Is.EqualTo(first.HasContact));
        Assert.That(second.SurfaceKind, Is.EqualTo(first.SurfaceKind));
    }

    private static void AssertBlank(ContactCombatCard card)
    {
        Assert.That(card.HasContact, Is.False);
        Assert.That(card.SurfaceKind, Is.EqualTo(ContactCombatCardTokens.SurfaceKind));
        Assert.That(card.ContactId, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Provenance, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Freshness, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Confidence, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Posture, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Targetability, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.EngagementStatus, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Bda, Is.EqualTo(ContactCombatCardTokens.Unknown));
        Assert.That(card.Fingerprint, Is.EqualTo(ContactCombatCardTokens.EmptyFingerprint));
        Assert.That(ContactCombatCardProjection.FingerprintMatches(card), Is.True);
    }

    private static ContactProvenanceSnapshot Provenance(
        ContactProvenanceConfidence confidence = ContactProvenanceConfidence.High,
        ContactProvenanceFreshness freshness = ContactProvenanceFreshness.Fresh,
        ulong age = 4,
        string targetId = "target-1",
        string knownTargetId = "target-1",
        ContactProvenanceQualityState quality = ContactProvenanceQualityState.None,
        bool outOfComms = false) =>
        new(new[] { ProvenanceRow(confidence: confidence, freshness: freshness, age: age, targetId: targetId, knownTargetId: knownTargetId, quality: quality, outOfComms: outOfComms) });

    private static ContactProvenanceState ProvenanceRow(
        string contactId = "c1",
        ContactProvenanceConfidence confidence = ContactProvenanceConfidence.High,
        ContactProvenanceFreshness freshness = ContactProvenanceFreshness.Fresh,
        ulong age = 4,
        string targetId = "target-1",
        string knownTargetId = "target-1",
        ContactProvenanceQualityState quality = ContactProvenanceQualityState.None,
        bool outOfComms = false) =>
        new(
            contactId,
            new ContactProvenanceSource("sensor-1", targetId, "observer:sensor-1|target:target-1"),
            confidence,
            freshness,
            age,
            new ContactProvenanceLastKnown("Identified", knownTargetId, 12, 1.5),
            outOfComms,
            quality);

    private static TargetabilityAcceptSnapshot Targetability(string targetId = "target-1") =>
        new(new[]
        {
            new TargetabilityAcceptContactRow(
                "c1",
                targetId,
                TargetabilityAcceptDisposition.Withheld,
                TargetabilityAcceptCauseCodes.Stale,
                null,
                null,
                Authority()),
        });

    private static BdaAssessSnapshot Bda(
        BdaAssessStateKind state = BdaAssessStateKind.Damaged,
        BdaAssessSourceKind source = BdaAssessSourceKind.PlatformDamage,
        string targetId = "target-1") =>
        new(new[] { BdaRow(targetId: targetId, state: state, source: source) });

    private static BdaAssessContactState BdaRow(
        string contactId = "c1",
        string targetId = "target-1",
        BdaAssessStateKind state = BdaAssessStateKind.Damaged,
        BdaAssessSourceKind source = BdaAssessSourceKind.PlatformDamage) =>
        new(contactId, targetId, "sensor-1", state, source, 21, 3.25, 3);

    private static AfterActionLedgerSnapshot Ledger(string outcome = "Hit") =>
        new(new[] { Entry(outcome: outcome) });

    private static AfterActionLedgerEntry Entry(
        string targetId = "target-1",
        ulong tick = 20,
        string outcome = "Hit",
        CombatEventPhaseConsume phase = CombatEventPhaseConsume.TerminalOutcome) =>
        new("u1", targetId, "asm", outcome, 9, 2.5, tick, phase, "ref-9");

    private static AfterActionLedgerEntry AttemptEntry(
        string shooterId,
        ulong correlationId,
        CombatEventPhaseConsume phase,
        string outcome,
        string explanationRef) =>
        new(shooterId, "target-1", "asm", outcome, correlationId, 2.5, 20, phase, explanationRef);

    private static ContactCombatCardPostureFact[] Postures() =>
        new[]
        {
            new ContactCombatCardPostureFact("contact-2", "raid"),
            new ContactCombatCardPostureFact("contact-1", "screen"),
        };

    private static C2AuthorityProjection Authority() =>
        new(
            new RoeProjection(RoeLevel.WeaponsTight, "WEAPONS_TIGHT", C2AuthorityDisposition.Withheld, "roe", false),
            new C2TargetingAuthority(C2AuthorityDisposition.Withheld, "authority-reason", null),
            Array.Empty<C2AuthorityActionState>());
}
