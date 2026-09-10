using NUnit.Framework;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

public sealed class CombatMapIntegrationTests
{
    [Test]
    public void Real_map_contact_identity_resolves_to_target_without_changing_symbol_picture()
    {
        var contacts = new[] { new ContactPictureEntry("contact-7", "target-2", "shooter", "Identified", 2, 2) };
        var symbols = MapPictureProjection.Project(new[] { new OobTreeEntry("shooter", true) }, contacts, 7);
        var poses = CombatMapSymbolBridge.Build(symbols, contacts);
        var events = new CombatEventSnapshot(new[] {
            new CombatEvent(CombatEventPhase.Firing, "shooter", "target-2", "Gun", "Launch", 1, 2, 2, "launch") });
        var map = CombatMapPresenter.Build(events, poses, 2, CombatZoomBand.Tactical);
        Assert.That(map.Effects, Has.Count.EqualTo(1));
        Assert.That(map.Effects[0].ToX, Is.EqualTo(symbols.Single(s => s.SymbolId == "contact-7").NormalizedX));
        Assert.That(symbols.Any(s => s.SymbolId == "target-2"), Is.False);
    }

    [TestCase(CombatZoomBand.Tactical)]
    [TestCase(CombatZoomBand.Operational)]
    [TestCase(CombatZoomBand.Theater)]
    public void Real_resolver_scenario_replays_same_map_and_inspectable_refusals(CombatZoomBand zoom)
    {
        var first = SliceBCombatScenario.Run(7);
        var replay = SliceBCombatScenario.Run(7);
        var refused = first.Events.Events.First(e => e.Phase == CombatEventPhase.AuthorizationRefused);
        var key = CombatMapPresenter.KeyFor(refused);
        var map = CombatMapPresenter.Build(first.Events, first.Symbols, 6, zoom, key);
        var repeated = CombatMapPresenter.Build(replay.Events, replay.Symbols, 6, zoom, key);
        Assert.That(repeated.Effects.Select(e => e.Label), Is.EqualTo(map.Effects.Select(e => e.Label)));
        Assert.That(repeated.EventLines, Is.EqualTo(map.EventLines));
        Assert.That(map.Effects.Any(e => e.Key == key && e.LinePattern == "none"), Is.True);
        var frame = CombatPresentationFrameBridge.Build(first.Log, SliceAContactFrame.Empty, 6);
        var explanation = CombatSelectionPresenter.Build(frame, key, null, null);
        Assert.That(explanation.StatusLine, Does.Contain("AuthorizationRefused"));
        Assert.That(explanation.CorrelationLine, Does.Contain(refused.CorrelationId.ToString()));
        Assert.That(map.Effects.Count, Is.LessThanOrEqualTo(64));
    }
}
