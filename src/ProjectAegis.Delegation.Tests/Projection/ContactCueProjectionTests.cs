using System.Reflection;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class ContactCueProjectionTests
{
    [Test]
    public void Empty_log_yields_empty_snapshot()
    {
        var snapshot = ContactCueProjection.Project(new DecisionLog(), currentSimTick: 0);

        Assert.That(snapshot.Contacts, Is.Empty);
        Assert.That(snapshot.UnitOrders, Is.Empty);
        Assert.That(ContactCueFingerprint.Compute(snapshot), Is.EqualTo("cue:empty"));
    }

    [Test]
    public void Stale_datalink_denied_and_unknown_position_stay_independent()
    {
        var log = LogWithContact(1, "Identified");
        log.AppendCommsStateChange(Denied(3));

        var snapshot = ContactCueProjection.Project(
            log,
            currentSimTick: StaleTick(1),
            origins: new[] { new ContactOriginFact("c1", ContactInformationOrigin.Datalink) });

        var row = snapshot.Contacts.Single();
        Assert.That(row.Confidence, Is.EqualTo(ContactProvenanceConfidence.High));
        Assert.That(row.Freshness, Is.EqualTo(ContactProvenanceFreshness.Stale));
        Assert.That(row.IsStale, Is.True);
        Assert.That(row.QualityState.HasFlag(ContactProvenanceQualityState.Stale), Is.True);
        Assert.That(row.Comms, Is.EqualTo(CommsState.Denied));
        Assert.That(row.OutOfCommsUnknown, Is.True);
        Assert.That(row.Origin, Is.EqualTo(ContactInformationOrigin.Datalink));
        Assert.That(row.IsDatalinkSourced, Is.True);
        Assert.That(row.Position, Is.EqualTo(ContactPositionClaim.Unknown));
        Assert.That(row.SimAgeTicks, Is.EqualTo(StaleTick(1) - 1));
    }

    [Test]
    public void Changing_one_axis_does_not_rewrite_the_others()
    {
        var staleTick = StaleTick(1);
        var denied = LogWithContact(1, "Identified");
        denied.AppendCommsStateChange(Denied(1));
        var datalink = ContactCueProjection.Project(
            denied,
            staleTick,
            origins: new[] { new ContactOriginFact("c1", ContactInformationOrigin.Datalink) });

        var nominal = LogWithContact(1, "Identified");
        var held = ContactCueProjection.Project(
            nominal,
            staleTick,
            positions: new[] { new ContactPositionSample("c1", 1, 0, 0, false, "origin-fixture") });

        var freshDenied = LogWithContact(staleTick, "Detected");
        freshDenied.AppendCommsStateChange(Denied(1));
        var low = ContactCueProjection.Project(
            freshDenied,
            staleTick,
            origins: new[] { new ContactOriginFact("c1", ContactInformationOrigin.Datalink) });

        Assert.That(held.Contacts[0].Confidence, Is.EqualTo(datalink.Contacts[0].Confidence));
        Assert.That(held.Contacts[0].IsStale, Is.True);
        Assert.That(held.Contacts[0].Comms, Is.EqualTo(CommsState.Nominal));
        Assert.That(held.Contacts[0].Origin, Is.EqualTo(ContactInformationOrigin.Unknown));
        Assert.That(held.Contacts[0].IsDatalinkSourced, Is.False);
        Assert.That(held.Contacts[0].Position.EastMeters, Is.EqualTo(0d));
        Assert.That(held.Contacts[0].Position.Kind, Is.EqualTo(ContactPositionClaimKind.FixtureOnly));

        Assert.That(low.Contacts[0].Confidence, Is.EqualTo(ContactProvenanceConfidence.Low));
        Assert.That(low.Contacts[0].Freshness, Is.EqualTo(ContactProvenanceFreshness.Fresh));
        Assert.That(low.Contacts[0].IsStale, Is.False);
        Assert.That(low.Contacts[0].Comms, Is.EqualTo(CommsState.Denied));
        Assert.That(low.Contacts[0].IsDatalinkSourced, Is.True);
        Assert.That(low.Contacts[0].Position, Is.EqualTo(ContactPositionClaim.Unknown));
    }

    [Test]
    public void Unknown_position_differs_from_a_zero_coordinate()
    {
        var missing = ContactCueProjection.Project(LogWithContact(2, "Classified"), 2);
        var origin = ContactCueProjection.Project(
            LogWithContact(2, "Classified"),
            2,
            positions: new[] { new ContactPositionSample("c1", 2, 0, 0, false, "origin-fixture") });

        Assert.That(missing.Contacts[0].Position.Kind, Is.EqualTo(ContactPositionClaimKind.Unknown));
        Assert.That(missing.Contacts[0].Position.EastMeters, Is.Null);
        Assert.That(missing.Contacts[0].Position.NorthMeters, Is.Null);
        Assert.That(origin.Contacts[0].Position.EastMeters, Is.EqualTo(0d));
        Assert.That(origin.Contacts[0].Position.NorthMeters, Is.EqualTo(0d));
        Assert.That(missing.Contacts[0].Position, Is.Not.EqualTo(origin.Contacts[0].Position));
        Assert.That(missing.Contacts[0].Confidence, Is.EqualTo(ContactProvenanceConfidence.Medium));
    }

    [Test]
    public void Sim_age_behind_the_last_report_is_unknown_not_zero()
    {
        var snapshot = ContactCueProjection.Project(
            LogWithContact(10, "Detected", simTime: 10),
            currentSimTick: 4,
            currentSimTime: 1);

        var row = snapshot.Contacts.Single();
        Assert.That(row.SimAgeTicks, Is.Null);
        Assert.That(row.AgeSimTime, Is.Null);
        Assert.That(row.SimAgeTicks, Is.Not.EqualTo(0UL));
    }

    [Test]
    public void Age_follows_sim_time_when_paused_accelerated_or_replayed()
    {
        var pausedA = ContactCueProjection.Project(LogWithContact(5, "Classified", 5), 5, currentSimTime: 5);
        var pausedB = ContactCueProjection.Project(LogWithContact(5, "Classified", 5), 5, currentSimTime: 5);
        var accelerated = ContactCueProjection.Project(LogWithContact(5, "Classified", 5), 40, currentSimTime: 25);
        var replayed = ContactCueProjection.Project(LogWithContact(5, "Classified", 5), 40, currentSimTime: 25);

        Assert.That(pausedA.Contacts[0].SimAgeTicks, Is.EqualTo(0UL));
        Assert.That(pausedA.Contacts[0].AgeSimTime, Is.EqualTo(0d));
        Assert.That(pausedB.Contacts[0].SimAgeTicks, Is.EqualTo(pausedA.Contacts[0].SimAgeTicks));
        Assert.That(pausedB.Contacts[0].AgeSimTime, Is.EqualTo(pausedA.Contacts[0].AgeSimTime));
        Assert.That(accelerated.Contacts[0].SimAgeTicks, Is.EqualTo(35UL));
        Assert.That(accelerated.Contacts[0].AgeSimTime, Is.EqualTo(20d));
        Assert.That(replayed.Contacts[0].SimAgeTicks, Is.EqualTo(accelerated.Contacts[0].SimAgeTicks));
        Assert.That(replayed.Contacts[0].AgeSimTime, Is.EqualTo(accelerated.Contacts[0].AgeSimTime));
        Assert.That(
            ContactCueFingerprint.Compute(accelerated),
            Is.EqualTo(ContactCueFingerprint.Compute(replayed)));
    }

    [Test]
    public void Omitted_sim_clock_does_not_report_zero_age_time()
    {
        var snapshot = ContactCueProjection.Project(LogWithContact(5, "Classified", 5), 8);

        Assert.That(snapshot.Contacts[0].SimAgeTicks, Is.EqualTo(3UL));
        Assert.That(snapshot.Contacts[0].AgeSimTime, Is.Null);
    }

    [Test]
    public void Comms_loss_does_not_infer_a_fresh_position()
    {
        var log = LogWithContact(6, "Identified", 6);
        log.AppendCommsStateChange(Denied(3));

        var snapshot = ContactCueProjection.Project(
            log,
            6,
            positions: new[] { new ContactPositionSample("c1", 4, 9, 9, false, "after-loss") });

        var row = snapshot.Contacts.Single();
        Assert.That(row.Freshness, Is.EqualTo(ContactProvenanceFreshness.Fresh));
        Assert.That(row.Comms, Is.EqualTo(CommsState.Denied));
        Assert.That(row.Position, Is.EqualTo(ContactPositionClaim.Unknown));
        Assert.That(row.Position.EastMeters, Is.Null);
        Assert.That(row.Position.IsFreshFix, Is.False);
    }

    [Test]
    public void Comms_loss_holds_the_pre_loss_fixture_and_drops_the_later_sample()
    {
        var log = LogWithContact(6, "Identified", 6);
        log.AppendCommsStateChange(Denied(3));

        var snapshot = ContactCueProjection.Project(
            log,
            6,
            positions: new[]
            {
                new ContactPositionSample("c1", 5, 9, 9, false, "after-loss"),
                new ContactPositionSample("c1", 2, 1, 2, false, "range-ring-fixture"),
            });

        var position = snapshot.Contacts.Single().Position;
        Assert.That(position.Kind, Is.EqualTo(ContactPositionClaimKind.FixtureOnly));
        Assert.That(position.EastMeters, Is.EqualTo(1d));
        Assert.That(position.NorthMeters, Is.EqualTo(2d));
        Assert.That(position.FixtureLabel, Is.EqualTo("range-ring-fixture"));
        Assert.That(position.IsFreshFix, Is.False);
        Assert.That(position.ReportedSimTick, Is.EqualTo(2UL));
    }

    [Test]
    public void Degraded_comms_can_hold_a_reported_sample_without_calling_it_fresh()
    {
        var log = LogWithContact(1, "Classified");
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 1, 1, "c2-net", CommsState.Nominal, CommsState.Degraded, "datalink-lag"));
        var display = new ScenarioCommsDisplaySettings(
            2, 0.06f, 0.04f, degradedOrderDelayTicks: 0, degradedStaleThresholdDivisor: 2);
        var staleTick = 1UL + (ContactProvenanceProjection.DefaultStaleThresholdTicks / 2) + 1;

        var snapshot = ContactCueProjection.Project(
            log,
            staleTick,
            commsDisplay: display,
            positions: new[] { new ContactPositionSample("c1", staleTick, 4, 5, false, "lagged-fixture") });

        var row = snapshot.Contacts.Single();
        Assert.That(row.IsStale, Is.True);
        Assert.That(row.Comms, Is.EqualTo(CommsState.Degraded));
        Assert.That(row.OutOfCommsUnknown, Is.False);
        Assert.That(row.Position.EastMeters, Is.EqualTo(4d));
        Assert.That(row.Position.IsFreshFix, Is.False);
        Assert.That(row.Position.FixtureLabel, Is.EqualTo("lagged-fixture"));
    }

    [Test]
    public void Identity_confidence_does_not_derive_a_numeric_ellipse()
    {
        foreach (ContactProvenanceConfidence confidence in Enum.GetValues<ContactProvenanceConfidence>())
        {
            var claim = ContactCueProjection.PositionFromIdentityConfidence(confidence);
            Assert.That(claim, Is.EqualTo(ContactPositionClaim.Unknown));
            Assert.That(claim.EastMeters, Is.Null);
            Assert.That(claim.NorthMeters, Is.Null);
        }

        var high = ContactCueProjection.Project(LogWithContact(2, "Identified"), 2);
        var low = ContactCueProjection.Project(LogWithContact(2, "Detected"), 2);
        Assert.That(high.Contacts[0].Confidence, Is.EqualTo(ContactProvenanceConfidence.High));
        Assert.That(low.Contacts[0].Confidence, Is.EqualTo(ContactProvenanceConfidence.Low));
        Assert.That(high.Contacts[0].Position, Is.EqualTo(low.Contacts[0].Position));
        Assert.That(high.Contacts[0].Position.Kind, Is.EqualTo(ContactPositionClaimKind.Unknown));
    }

    [Test]
    public void Catalog_platform_position_is_not_used_as_a_contact_fix()
    {
        var catalog = new InMemoryCatalogReader(
            Array.Empty<CatalogSensorBinding>(),
            platforms: new[] { new CatalogPlatformEntry("hostile-1", 59, 24, 10, "surface") });

        var snapshot = ContactCueProjection.Project(
            LogWithContact(2, "Detected"),
            2,
            catalog: catalog);

        Assert.That(snapshot.Contacts[0].Position, Is.EqualTo(ContactPositionClaim.Unknown));
        Assert.That(snapshot.Contacts[0].QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss), Is.False);
    }

    [Test]
    public void Fixture_geometry_is_labeled_and_a_blank_label_is_not_a_live_claim()
    {
        var snapshot = ContactCueProjection.Project(
            LogWithContact(3, "Detected"),
            3,
            positions: new[]
            {
                new ContactPositionSample("c1", 3, 12, 4, false, "  "),
            });

        var position = snapshot.Contacts.Single().Position;
        Assert.That(position.Kind, Is.EqualTo(ContactPositionClaimKind.FixtureOnly));
        Assert.That(position.FixtureLabel, Is.EqualTo(ContactCueProjection.FixtureOnlyLabel));
        Assert.That(position.IsFreshFix, Is.False);
        Assert.That(position.EastMeters, Is.EqualTo(12d));
    }

    [Test]
    public void Production_feed_flag_stays_labeled_until_the_feed_is_wired()
    {
        Assert.That(ContactCueProjection.ProductionPositionFeedWired, Is.False);

        var snapshot = ContactCueProjection.Project(
            LogWithContact(3, "Identified"),
            3,
            positions: new[] { new ContactPositionSample("c1", 3, 8, 7, true, "live-track") });

        var position = snapshot.Contacts.Single().Position;
        Assert.That(position.Kind, Is.EqualTo(ContactPositionClaimKind.FixtureOnly));
        Assert.That(position.FixtureLabel, Is.EqualTo(ContactCueProjection.ProductionFeedUnavailableLabel));
        Assert.That(position.IsFreshFix, Is.False);
        Assert.That(position.EastMeters, Is.EqualTo(8d));
        Assert.That(position.NorthMeters, Is.EqualTo(7d));
    }

    [Test]
    public void Wired_production_feed_is_last_reported_and_still_has_no_ellipse()
    {
        var snapshot = ContactCueProjection.Project(
            LogWithContact(4, "Identified"),
            4,
            positions: new[] { new ContactPositionSample("c1", 4, 3, 6, true) },
            productionPositionFeedWired: true);

        var position = snapshot.Contacts.Single().Position;
        Assert.That(position.Kind, Is.EqualTo(ContactPositionClaimKind.LastReportedHeld));
        Assert.That(position.FixtureLabel, Is.Null);
        Assert.That(position.IsFreshFix, Is.True);
        Assert.That(position.EastMeters, Is.EqualTo(3d));
        Assert.That(position.NorthMeters, Is.EqualTo(6d));
        Assert.That(snapshot.Contacts[0].Confidence, Is.EqualTo(ContactProvenanceConfidence.High));
    }

    [Test]
    public void Wired_production_feed_does_not_refresh_during_comms_loss()
    {
        var log = LogWithContact(6, "Identified", 6);
        log.AppendCommsStateChange(Denied(3));

        var snapshot = ContactCueProjection.Project(
            log,
            6,
            positions: new[]
            {
                new ContactPositionSample("c1", 2, 4, 5, true),
                new ContactPositionSample("c1", 5, 9, 8, true),
            },
            productionPositionFeedWired: true);

        var position = snapshot.Contacts.Single().Position;
        Assert.That(position.Kind, Is.EqualTo(ContactPositionClaimKind.LastReportedHeld));
        Assert.That(position.EastMeters, Is.EqualTo(4d));
        Assert.That(position.NorthMeters, Is.EqualTo(5d));
        Assert.That(position.IsFreshFix, Is.False);
        Assert.That(position.FixtureLabel, Is.Null);
    }

    [Test]
    public void Future_and_conflicting_samples_do_not_become_a_fix()
    {
        var future = ContactCueProjection.Project(
            LogWithContact(4, "Detected"),
            4,
            positions: new[] { new ContactPositionSample("c1", 10, 3, 3, false, "later") });
        var conflict = ContactCueProjection.Project(
            LogWithContact(4, "Detected"),
            4,
            positions: new[]
            {
                new ContactPositionSample("c1", 4, 1, 1, false, "a"),
                new ContactPositionSample("c1", 4, 2, 2, false, "b"),
            });

        Assert.That(future.Contacts[0].Position, Is.EqualTo(ContactPositionClaim.Unknown));
        Assert.That(conflict.Contacts[0].Position, Is.EqualTo(ContactPositionClaim.Unknown));
    }

    [Test]
    public void Conflicting_origins_are_unknown_and_organic_is_not_datalink()
    {
        var conflict = ContactCueProjection.Project(
            LogWithContact(2, "Detected"),
            2,
            origins: new[]
            {
                new ContactOriginFact("c1", ContactInformationOrigin.Datalink),
                new ContactOriginFact("c1", ContactInformationOrigin.Organic),
            });
        var organic = ContactCueProjection.Project(
            LogWithContact(2, "Detected"),
            2,
            origins: new[] { new ContactOriginFact("c1", ContactInformationOrigin.Organic) });

        Assert.That(conflict.Contacts[0].Origin, Is.EqualTo(ContactInformationOrigin.Unknown));
        Assert.That(conflict.Contacts[0].IsDatalinkSourced, Is.False);
        Assert.That(organic.Contacts[0].Origin, Is.EqualTo(ContactInformationOrigin.Organic));
        Assert.That(organic.Contacts[0].IsDatalinkSourced, Is.False);
    }

    [Test]
    public void Due_player_order_is_the_only_genuinely_pending_command()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(5, "u1", OrderKind.Engage));

        var row = ContactCueProjection.Project(log, 5).UnitOrders.Single();

        Assert.That(row.UnitId, Is.EqualTo("u1"));
        Assert.That(row.Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(row.IsGenuinelyPending, Is.True);
        Assert.That(row.Kind, Is.EqualTo(OrderKind.Engage));
        Assert.That(row.IssuedSimTick, Is.EqualTo(5UL));
        Assert.That(row.ExecuteSimTick, Is.EqualTo(5UL));
    }

    [Test]
    public void Queued_fact_without_a_player_order_is_not_pending()
    {
        var snapshot = ContactCueProjection.Project(
            new DecisionLog(),
            5,
            terminalOrders: new[]
            {
                new UnitOrderTerminalFact("u1", 5, UnitOrderDisposition.Queued, OrderKind.Hold),
            });

        Assert.That(snapshot.UnitOrders, Is.Empty);
    }

    [Test]
    public void Delayed_order_is_not_a_pending_command()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Move, executeSimTick: 20));

        var row = ContactCueProjection.Project(log, 5).UnitOrders.Single();

        Assert.That(row.Disposition, Is.EqualTo(UnitOrderDisposition.Delayed));
        Assert.That(row.IsGenuinelyPending, Is.False);
        Assert.That(row.ExecuteSimTick, Is.EqualTo(20UL));
    }

    [Test]
    public void Rejected_order_is_not_a_pending_command()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Engage));
        log.AppendPolicyDenial(Denial(4, "u1", OrderKind.Engage));

        var row = ContactCueProjection.Project(log, 6).UnitOrders.Single();

        Assert.That(row.Disposition, Is.EqualTo(UnitOrderDisposition.Rejected));
        Assert.That(row.IsGenuinelyPending, Is.False);
        Assert.That(row.Kind, Is.EqualTo(OrderKind.Engage));
    }

    [Test]
    public void Same_tick_denial_rejects_instead_of_queuing()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(3, "u1", OrderKind.Hold));
        log.AppendPolicyDenial(Denial(3, "u1", OrderKind.Hold));

        var row = ContactCueProjection.Project(log, 3).UnitOrders.Single();

        Assert.That(row.Disposition, Is.EqualTo(UnitOrderDisposition.Rejected));
        Assert.That(row.IsGenuinelyPending, Is.False);
    }

    [Test]
    public void Expired_order_is_not_pending_and_age_alone_does_not_expire()
    {
        var aged = new DecisionLog();
        aged.AppendPlayerOrder(PlayerOrder(1, "u1", OrderKind.Engage));
        var stillPending = ContactCueProjection.Project(aged, 5_000).UnitOrders.Single();

        var wrongKind = new DecisionLog();
        wrongKind.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Engage));
        var notExpired = ContactCueProjection.Project(
            wrongKind,
            10,
            terminalOrders: new[] { new UnitOrderTerminalFact("u1", 8, UnitOrderDisposition.Expired, OrderKind.Hold) });

        var expired = new DecisionLog();
        expired.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Engage));
        var snapshot = ContactCueProjection.Project(
            expired,
            10,
            terminalOrders: new[] { new UnitOrderTerminalFact("u1", 8, UnitOrderDisposition.Expired, OrderKind.Engage) });

        Assert.That(stillPending.Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(stillPending.IsGenuinelyPending, Is.True);
        Assert.That(notExpired.UnitOrders.Single().Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(snapshot.UnitOrders.Single().Disposition, Is.EqualTo(UnitOrderDisposition.Expired));
        Assert.That(snapshot.UnitOrders.Single().IsGenuinelyPending, Is.False);
    }

    [Test]
    public void Future_order_and_mismatched_denial_do_not_clear_a_pending_command()
    {
        var future = new DecisionLog();
        future.AppendPlayerOrder(PlayerOrder(20, "u1", OrderKind.Engage));

        var mismatched = new DecisionLog();
        mismatched.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Engage));
        mismatched.AppendPolicyDenial(Denial(4, "u1", OrderKind.Hold));

        Assert.That(ContactCueProjection.Project(future, 5).UnitOrders, Is.Empty);
        var pending = ContactCueProjection.Project(mismatched, 6).UnitOrders.Single();
        Assert.That(pending.Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(pending.IsGenuinelyPending, Is.True);
    }

    [Test]
    public void Launched_engagement_executes_only_a_matching_engage_order()
    {
        var launched = new DecisionLog();
        launched.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Engage));
        launched.AppendEngagement(Engagement(4, "u1", launched: true));

        var aborted = new DecisionLog();
        aborted.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Engage));
        aborted.AppendEngagement(Engagement(4, "u1", launched: false));

        var move = new DecisionLog();
        move.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Move));
        move.AppendEngagement(Engagement(4, "u1", launched: true));

        Assert.That(ContactCueProjection.Project(launched, 6).UnitOrders.Single().Disposition, Is.EqualTo(UnitOrderDisposition.Executed));
        Assert.That(ContactCueProjection.Project(launched, 6).UnitOrders.Single().IsGenuinelyPending, Is.False);
        Assert.That(ContactCueProjection.Project(aborted, 6).UnitOrders.Single().Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(ContactCueProjection.Project(move, 6).UnitOrders.Single().Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(ContactCueProjection.Project(move, 6).UnitOrders.Single().Kind, Is.EqualTo(OrderKind.Move));
    }

    [Test]
    public void Explicit_executed_fact_covers_orders_the_engagement_log_does_not()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Hold));

        var row = ContactCueProjection.Project(
            log,
            4,
            terminalOrders: new[] { new UnitOrderTerminalFact("u1", 4, UnitOrderDisposition.Executed, OrderKind.Hold) })
            .UnitOrders.Single();

        Assert.That(row.Disposition, Is.EqualTo(UnitOrderDisposition.Executed));
        Assert.That(row.IsGenuinelyPending, Is.False);
    }

    [Test]
    public void Other_unit_denial_and_engagement_do_not_change_this_unit()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Engage));
        log.AppendPolicyDenial(Denial(3, "u2", OrderKind.Engage));
        log.AppendEngagement(Engagement(4, "u3", launched: true));

        var snapshot = ContactCueProjection.Project(log, 6);
        var pending = snapshot.UnitOrders.Single(order => order.UnitId == "u1");
        var rejected = snapshot.UnitOrders.Single(order => order.UnitId == "u2");
        var executed = snapshot.UnitOrders.Single(order => order.UnitId == "u3");

        Assert.That(pending.Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(pending.IsGenuinelyPending, Is.True);
        Assert.That(rejected.Disposition, Is.EqualTo(UnitOrderDisposition.Rejected));
        Assert.That(rejected.IsGenuinelyPending, Is.False);
        Assert.That(executed.Disposition, Is.EqualTo(UnitOrderDisposition.Executed));
        Assert.That(executed.Kind, Is.EqualTo(OrderKind.Engage));
        Assert.That(executed.IsGenuinelyPending, Is.False);
    }

    [Test]
    public void Newer_command_replaces_an_older_pending_order()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Engage));
        log.AppendPlayerOrder(PlayerOrder(6, "u1", OrderKind.Engage));
        log.AppendEngagement(Engagement(7, "u1", launched: true));

        var row = ContactCueProjection.Project(log, 8).UnitOrders.Single();

        Assert.That(row.Disposition, Is.EqualTo(UnitOrderDisposition.Executed));
        Assert.That(row.IssuedSimTick, Is.EqualTo(6UL));
        Assert.That(row.IsGenuinelyPending, Is.False);
    }

    [Test]
    public void Denial_before_the_latest_order_does_not_reject_it()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Engage));
        log.AppendPolicyDenial(Denial(2, "u1", OrderKind.Engage));
        log.AppendPlayerOrder(PlayerOrder(5, "u1", OrderKind.Engage));

        var row = ContactCueProjection.Project(log, 6).UnitOrders.Single();

        Assert.That(row.Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(row.IssuedSimTick, Is.EqualTo(5UL));
        Assert.That(row.IsGenuinelyPending, Is.True);
    }

    [Test]
    public void Same_tick_reissue_stays_pending_after_earlier_denial()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(3, "u1", OrderKind.Hold));
        log.AppendPolicyDenial(Denial(3, "u1", OrderKind.Hold));
        log.AppendPlayerOrder(PlayerOrder(3, "u1", OrderKind.Hold));

        var row = ContactCueProjection.Project(log, 3).UnitOrders.Single();

        Assert.That(row.Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(row.IsGenuinelyPending, Is.True);
        Assert.That(row.IssuedSimTick, Is.EqualTo(3UL));
    }

    [Test]
    public void Same_tick_reissue_stays_pending_after_earlier_launch()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(PlayerOrder(3, "u1", OrderKind.Engage));
        log.AppendEngagement(Engagement(3, "u1", launched: true));
        log.AppendPlayerOrder(PlayerOrder(3, "u1", OrderKind.Engage));

        var row = ContactCueProjection.Project(log, 3).UnitOrders.Single();

        Assert.That(row.Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(row.IsGenuinelyPending, Is.True);
        Assert.That(row.IssuedSimTick, Is.EqualTo(3UL));
    }

    [Test]
    public void Selection_change_does_not_keep_another_contact_or_unit()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendContactChange(Change(4, "c2", "hostile-2", "Unknown", "Detected"));
        log.AppendPlayerOrder(PlayerOrder(4, "u1", OrderKind.Engage));
        log.AppendPlayerOrder(PlayerOrder(4, "u2", OrderKind.Hold, executeSimTick: 9));
        var snapshot = ContactCueProjection.Project(
            log,
            4,
            origins: new[]
            {
                new ContactOriginFact("c1", ContactInformationOrigin.Datalink),
                new ContactOriginFact("c2", ContactInformationOrigin.Organic),
            });

        var first = ContactCueProjection.Select(snapshot, "c1", "u1");
        var second = ContactCueProjection.Select(snapshot, "c2", "u2");
        var missing = ContactCueProjection.Select(snapshot, "c9", "u9");
        var blank = ContactCueProjection.Select(snapshot, " ", null);

        Assert.That(first.ContactId, Is.EqualTo("c1"));
        Assert.That(first.Contact!.IsDatalinkSourced, Is.True);
        Assert.That(first.Contact.IsStale, Is.False);
        Assert.That(first.UnitOrder!.Disposition, Is.EqualTo(UnitOrderDisposition.Queued));
        Assert.That(second.ContactId, Is.EqualTo("c2"));
        Assert.That(second.Contact!.IsDatalinkSourced, Is.False);
        Assert.That(second.Contact.Confidence, Is.EqualTo(ContactProvenanceConfidence.Low));
        Assert.That(second.UnitId, Is.EqualTo("u2"));
        Assert.That(second.UnitOrder!.Disposition, Is.EqualTo(UnitOrderDisposition.Delayed));
        Assert.That(first.ContactId, Is.EqualTo("c1"));
        Assert.That(first.UnitOrder!.IsGenuinelyPending, Is.True);
        Assert.That(missing.Contact, Is.Null);
        Assert.That(missing.UnitOrder, Is.Null);
        Assert.That(missing.ContactId, Is.Null);
        Assert.That(blank.Contact, Is.Null);
        Assert.That(snapshot.Contacts, Has.Count.EqualTo(2));
        Assert.That(snapshot.UnitOrders, Has.Count.EqualTo(2));
    }

    [Test]
    public void Disagreeing_duplicate_rows_fail_closed_on_select()
    {
        var left = new ContactCueState(
            "c1",
            ContactProvenanceConfidence.High,
            ContactProvenanceFreshness.Fresh,
            0,
            null,
            CommsState.Nominal,
            false,
            ContactProvenanceQualityState.None,
            ContactInformationOrigin.Datalink,
            true,
            false,
            ContactPositionClaim.Unknown);
        var right = left with { Confidence = ContactProvenanceConfidence.Low, IsDatalinkSourced = false };
        var snapshot = new ContactCueSnapshot(new[] { left, right }, Array.Empty<UnitOrderQueueState>());

        var selected = ContactCueProjection.Select(snapshot, "c1", "u1");

        Assert.That(selected.Contact, Is.Null);
        Assert.That(selected.UnitOrder, Is.Null);
    }

    [Test]
    public void Projection_does_not_mutate_the_log_or_keep_static_selection_state()
    {
        var log = LogWithContact(2, "Detected");
        log.AppendPlayerOrder(PlayerOrder(2, "u1", OrderKind.Hold));
        var before = log.ChronologicalEntries().Count;

        var snapshot = ContactCueProjection.Project(log, 2);
        ContactCueProjection.Select(snapshot, "c1", "u1");

        Assert.That(log.ChronologicalEntries().Count, Is.EqualTo(before));
        var cached = typeof(ContactCueProjection)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => !field.IsLiteral);
        Assert.That(cached, Is.Empty);
    }

    [Test]
    public void Cue_types_omit_ui_icon_badge_and_ellipse_fields()
    {
        var types = new[]
        {
            typeof(ContactCueState),
            typeof(ContactCueSnapshot),
            typeof(ContactPositionClaim),
            typeof(UnitOrderQueueState),
            typeof(ContactCueSelection),
        };
        string[] forbidden =
        [
            "selection", "hover", "camera", "visible", "visibility", "selected",
            "icon", "badge", "sprite", "ellipse", "radius", "color",
        ];

        foreach (var type in types)
        {
            foreach (var property in type.GetProperties())
            {
                var name = property.Name.ToLowerInvariant();
                Assert.That(
                    forbidden.Any(token => name.Contains(token, StringComparison.Ordinal)),
                    Is.False,
                    $"{type.Name}.{property.Name} is presentation chrome");
            }
        }
    }

    [Test]
    public void Identical_inputs_yield_the_pinned_fingerprint()
    {
        var log = LogWithContact(5, "Identified", 5);
        log.AppendPlayerOrder(PlayerOrder(5, "u1", OrderKind.Engage));
        var origins = new[] { new ContactOriginFact("c1", ContactInformationOrigin.Datalink) };

        var a = ContactCueProjection.Project(log, 5, origins: origins);
        var b = ContactCueProjection.Project(log, 5, origins: origins);

        const string expected =
            "cue:c=1;u=1|c1,3,0,0,-,0,0,0,2,1,0,0,-,-,-,-,0|u1,1,2,5,5,1";
        Assert.That(ContactCueFingerprint.Compute(a), Is.EqualTo(expected));
        Assert.That(ContactCueFingerprint.Compute(b), Is.EqualTo(ContactCueFingerprint.Compute(a)));
        Assert.That(
            ContactProvenanceFingerprint.Compute(ContactProvenanceProjection.Project(log, 5)),
            Is.EqualTo(ContactProvenanceFingerprint.Compute(ContactProvenanceProjection.Project(log, 5))));
    }

    private static ulong StaleTick(ulong lastTick) =>
        lastTick + ContactProvenanceProjection.DefaultStaleThresholdTicks + 1;

    private static DecisionLog LogWithContact(ulong tick, string lifecycle, double? simTime = null)
    {
        var log = new DecisionLog();
        var time = simTime ?? tick;
        log.AppendContactChange(new ContactChangeRecord(
            0, time, tick, "u1", "c1", "hostile-1", "Unknown", lifecycle));
        return log;
    }

    private static ContactChangeRecord Change(
        ulong tick,
        string contactId,
        string targetId,
        string previous,
        string next) =>
        new(0, tick, tick, "u1", contactId, targetId, previous, next);

    private static PlayerOrderRecord PlayerOrder(
        ulong tick,
        string unitId,
        OrderKind kind,
        ulong? executeSimTick = null) =>
        new(0, tick, tick, new TargetId(unitId), kind, ExecuteSimTick: executeSimTick ?? tick);

    private static PolicyDenialRecord Denial(ulong tick, string unitId, OrderKind kind) =>
        new(0, tick, tick, new AgentId(unitId), new TargetId(unitId), 1, FireAbortReason.WeaponsTight, kind);

    private static EngagementRecord Engagement(ulong tick, string unitId, bool launched) =>
        new(0, tick, tick, new TargetId(unitId), 1, launched, launched ? null : "aborted", new TargetId("hostile-1"), "Missile");

    private static CommsStateChangeRecord Denied(ulong tick) =>
        new(0, tick, tick, "c2-net", CommsState.Nominal, CommsState.Denied, "jam");
}
