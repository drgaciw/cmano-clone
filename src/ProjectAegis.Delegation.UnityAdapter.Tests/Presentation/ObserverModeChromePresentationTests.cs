namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using System;
using Controllers;
using Input;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

/// <summary>DRG-249 / MODE-03: AttachReplayViewer observer chrome.</summary>
[TestFixture]
public sealed class ObserverModeChromePresentationTests
{
    [Test]
    public void Viewer_attached_projects_observer_read_only_chrome_with_commands_disabled()
    {
        var bridge = BridgeWithHumanUnit();
        bridge.AttachReplayViewer = true;

        var chrome = ObserverModeChromePresentation.Project(bridge);

        Assert.That(chrome.IsObserver, Is.True);
        Assert.That(chrome.ModeLabel, Is.EqualTo(ObserverModeChromePresentation.ObserverLabel));
        Assert.That(chrome.ModeLabel, Does.Contain("OBSERVER").And.Contain("READ-ONLY"));
        Assert.That(chrome.CssClass, Is.EqualTo(ObserverModeChromePresentation.ObserverCssClass));
        Assert.That(chrome.CanIssueOrders, Is.False);
        Assert.That(chrome.CanDelegate, Is.False);
        Assert.That(chrome.DisabledReasonCode, Is.EqualTo(C2PlayerCommandBridge.ReasonReplayAttached));
        Assert.That(chrome.DisabledReasonText, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Viewer_attached_order_attempt_returns_false_with_visible_reason_and_logs_nothing()
    {
        var bridge = BridgeWithHumanUnit();
        bridge.AttachReplayViewer = true;

        var attempt = ObserverModeChromePresentation.TryIssueOrder(bridge, new EntityKey(1), "hold", simTime: 1);

        Assert.That(attempt.Accepted, Is.False);
        Assert.That(attempt.ReasonCode, Is.EqualTo(C2PlayerCommandBridge.ReasonReplayAttached));
        Assert.That(attempt.ReasonText, Is.EqualTo(ObserverModeChromePresentation.ReplayAttachedReasonText));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Viewer_attached_chrome_matches_delegation_authority_refusal()
    {
        var bridge = BridgeWithHumanUnit();
        bridge.Registry.TryGetBinding(new EntityKey(1), out var binding);
        binding.Target.Slot.SetActive(null);
        bridge.AttachReplayViewer = true;

        var chrome = ObserverModeChromePresentation.Project(bridge);

        Assert.That(chrome.CanDelegate, Is.False);
        Assert.That(bridge.TryTakeDirectControl(new EntityKey(1), simTime: 1), Is.False);
    }

    [Test]
    public void Flag_off_restores_command_chrome_and_order_issuance()
    {
        var bridge = BridgeWithHumanUnit();
        bridge.AttachReplayViewer = true;
        Assert.That(ObserverModeChromePresentation.Project(bridge).IsObserver, Is.True);

        bridge.AttachReplayViewer = false;
        var chrome = ObserverModeChromePresentation.Project(bridge);
        var attempt = ObserverModeChromePresentation.TryIssueOrder(bridge, new EntityKey(1), "hold", simTime: 2);

        Assert.That(chrome.IsObserver, Is.False);
        Assert.That(chrome.ModeLabel, Is.EqualTo(ObserverModeChromePresentation.CommandLabel));
        Assert.That(chrome.CssClass, Is.EqualTo(ObserverModeChromePresentation.CommandCssClass));
        Assert.That(chrome.CanIssueOrders, Is.True);
        Assert.That(chrome.CanDelegate, Is.True);
        Assert.That(chrome.DisabledReasonCode, Is.Null);
        Assert.That(chrome.DisabledReasonText, Is.Null);
        Assert.That(attempt.Accepted, Is.True);
        Assert.That(attempt.ReasonCode, Is.Null);
        Assert.That(attempt.ReasonText, Is.Null);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(1));
    }

    [Test]
    public void Projecting_chrome_does_not_change_viewer_flag()
    {
        var bridge = BridgeWithHumanUnit();
        bridge.AttachReplayViewer = true;

        _ = ObserverModeChromePresentation.Project(bridge);

        Assert.That(bridge.AttachReplayViewer, Is.True);
    }

    [Test]
    public void Non_observer_refusals_keep_their_reason_code_with_visible_text()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");

        var unknownCommand = ObserverModeChromePresentation.TryIssueOrder(bridge, new EntityKey(1), "warp", 1);
        var notHuman = ObserverModeChromePresentation.TryIssueOrder(bridge, new EntityKey(1), "hold", 1);
        var unknownUnit = ObserverModeChromePresentation.TryIssueOrder(bridge, new EntityKey(99), "hold", 1);

        Assert.That(unknownCommand.ReasonCode, Is.EqualTo(C2CommandIssuance.ReasonUnknownCommand));
        Assert.That(notHuman.ReasonCode, Is.EqualTo(C2PlayerCommandBridge.ReasonNotHumanControl));
        Assert.That(unknownUnit.ReasonCode, Is.EqualTo(C2PlayerCommandBridge.ReasonUnknownUnit));
        Assert.That(new[] { unknownCommand, notHuman, unknownUnit },
            Has.All.Property(nameof(ObserverOrderAttempt.Accepted)).False);
        Assert.That(new[] { unknownCommand.ReasonText, notHuman.ReasonText, unknownUnit.ReasonText },
            Has.None.Null.And.None.Empty);
    }

    [Test]
    public void Project_from_flag_matches_project_from_bridge()
    {
        Assert.That(ObserverModeChromePresentation.Project(attachReplayViewer: true),
            Is.EqualTo(ObserverModeChromePresentation.Project(BridgeWith(true))));
        Assert.That(ObserverModeChromePresentation.Project(attachReplayViewer: false),
            Is.EqualTo(ObserverModeChromePresentation.Project(BridgeWith(false))));
    }

    [Test]
    public void Null_bridge_throws()
    {
        Assert.Throws<ArgumentNullException>(() => ObserverModeChromePresentation.Project(null!));
        Assert.Throws<ArgumentNullException>(() =>
            ObserverModeChromePresentation.TryIssueOrder(null!, new EntityKey(1), "hold", 1));
    }

    private static DelegationBridge BridgeWith(bool viewer)
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false) { AttachReplayViewer = viewer };
        return bridge;
    }

    private static DelegationBridge BridgeWithHumanUnit()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());
        return bridge;
    }
}
