namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

using Controllers;
using Core;
using MissionIntent;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using Targets;

/// <summary>Inputs for assisted withdraw / redeploy confirm chrome (presentation-only; ADR-010 §2–3).</summary>
public sealed record CoordinationAssistedWithdrawRedeployConfirmInput(
    AutonomyLevel DelegationAutonomy,
    string GroupId,
    CoordinationDecision Decision,
    MissionIntentSnapshot? ReviewedIntent);

/// <summary>Text chrome for confirm / cancel affordances (never color-only).</summary>
public sealed record CoordinationAssistedWithdrawRedeployChrome(
    string Title,
    string Body,
    string ConfirmLabel,
    string CancelLabel);

/// <summary>Outcome of an explicit confirm action.</summary>
public sealed record CoordinationAssistedWithdrawRedeployConfirmResult(
    bool Accepted,
    string StatusLine);

/// <summary>Panel label text for assisted withdraw / redeploy confirm chrome.</summary>
public sealed record CoordinationAssistedWithdrawRedeployPanelLabels(
    string Title,
    string Body,
    string ConfirmLabel,
    string CancelLabel);

/// <summary>Panel status text after confirm.</summary>
public sealed record CoordinationAssistedWithdrawRedeployConfirmResultLabels(
    bool Accepted,
    string StatusLine);

/// <summary>Panel-readable staged request fields.</summary>
public sealed record CoordinationAssistedWithdrawRedeployPendingLabels(
    string GroupId,
    CoordinationDecision Decision,
    MissionIntentSnapshot? ReviewedIntent,
    bool RedeploySuggestion);

/// <summary>Panel-readable confirm input fields.</summary>
public sealed record CoordinationAssistedWithdrawRedeployConfirmInputLabels(
    AutonomyLevel DelegationAutonomy,
    string GroupId,
    CoordinationDecision Decision,
    MissionIntentSnapshot? ReviewedIntent);

/// <summary>Maps presentation DTOs to panel strings (headless; satisfies in-solution property reads).</summary>
public static class CoordinationAssistedWithdrawRedeployPanelBinder
{
    /// <summary>Binds confirm chrome for UI Toolkit labels and buttons.</summary>
    public static CoordinationAssistedWithdrawRedeployPanelLabels Bind(
        CoordinationAssistedWithdrawRedeployChrome chrome) =>
        new(chrome.Title, chrome.Body, chrome.ConfirmLabel, chrome.CancelLabel);

    /// <summary>Binds confirm outcome for status labels.</summary>
    public static CoordinationAssistedWithdrawRedeployConfirmResultLabels Bind(
        CoordinationAssistedWithdrawRedeployConfirmResult result) =>
        new(result.Accepted, result.StatusLine);

    /// <summary>Binds staged pending request for host visibility and diagnostics.</summary>
    public static CoordinationAssistedWithdrawRedeployPendingLabels Bind(
        CoordinationAssistedWithdrawRedeployPendingRequest pending) =>
        new(pending.GroupId, pending.Decision, pending.ReviewedIntent, pending.RedeploySuggestion);

    /// <summary>Binds confirm input for host staging calls.</summary>
    public static CoordinationAssistedWithdrawRedeployConfirmInputLabels Bind(
        CoordinationAssistedWithdrawRedeployConfirmInput input) =>
        new(input.DelegationAutonomy, input.GroupId, input.Decision, input.ReviewedIntent);
}

/// <summary>
/// Stages assisted withdraw / redeploy confirmation without mutating the order log until confirm.
/// Confirm must route through the existing command-review façade (e.g. <c>CoordinationCommandBridge</c>).
/// </summary>
public static class CoordinationAssistedWithdrawRedeployConfirmBinder
{
    private static CoordinationAssistedWithdrawRedeployPendingRequest? _pending;

    /// <summary>Active staged request, if any.</summary>
    public static CoordinationAssistedWithdrawRedeployPendingRequest? Pending => _pending;

    /// <summary>Clears staged state (tests and host teardown).</summary>
    public static void ResetForTests() => _pending = null;

    /// <summary>Whether the player must confirm before the command façade may enqueue.</summary>
    public static bool RequiresExplicitConfirm(AutonomyLevel delegationAutonomy, CoordinationDecision decision) =>
        delegationAutonomy == AutonomyLevel.Assisted && decision == CoordinationDecision.Withdraw;

    /// <summary>Resolves delegation autonomy from suspended agents on group members (human override path).</summary>
    public static AutonomyLevel ResolveDelegationAutonomy(DelegationBridge? bridge, string groupId)
    {
        if (bridge is null || string.IsNullOrWhiteSpace(groupId))
        {
            return AutonomyLevel.Manual;
        }

        var groupBinding = bridge.Registry.Bindings.FirstOrDefault(b =>
            b.Target is GroupTarget group
            && string.Equals(group.Id.Value, groupId, StringComparison.Ordinal));
        if (groupBinding?.Target is not GroupTarget groupTarget)
        {
            return AutonomyLevel.Manual;
        }

        AutonomyLevel resolved = AutonomyLevel.Manual;
        for (var i = 0; i < groupTarget.Members.Count; i++)
        {
            if (!bridge.Registry.TryGetBinding(groupTarget.Members[i], out var memberBinding)
                || memberBinding.Target is not UnitTarget unit)
            {
                continue;
            }

            var agent = unit.Slot.SuspendedAgent
                ?? unit.Slot.Active as AgentController;
            if (agent is null)
            {
                continue;
            }

            resolved = (AutonomyLevel)Math.Max((int)resolved, (int)agent.Autonomy);
        }

        return resolved;
    }

    /// <summary>Stages a withdraw / redeploy request without touching the order log.</summary>
    public static bool TryBegin(
        CoordinationAssistedWithdrawRedeployConfirmInput input,
        out CoordinationAssistedWithdrawRedeployChrome chrome)
    {
        if (input is null)
        {
            chrome = EmptyChrome();
            return false;
        }

        if (!RequiresExplicitConfirm(input.DelegationAutonomy, input.Decision))
        {
            chrome = EmptyChrome();
            return false;
        }

        if (string.IsNullOrWhiteSpace(input.GroupId))
        {
            chrome = EmptyChrome();
            return false;
        }

        var redeploySuggestion = input.ReviewedIntent?.AdvisoryRetask == MissionIntentRetaskAdvice.Withdraw;
        _pending = new CoordinationAssistedWithdrawRedeployPendingRequest(
            input.GroupId,
            input.Decision,
            input.ReviewedIntent,
            redeploySuggestion);
        chrome = BuildChrome(_pending);
        return true;
    }

    /// <summary>Builds chrome for the active staged request without mutating session state.</summary>
    public static CoordinationAssistedWithdrawRedeployChrome? ProjectChromeForPending()
    {
        if (_pending is null)
        {
            return null;
        }

        return BuildChrome(_pending);
    }

    /// <summary>Dismisses staged confirm without order-log mutation.</summary>
    public static bool TryCancel()
    {
        if (_pending is null)
        {
            return false;
        }

        _pending = null;
        return true;
    }

    /// <summary>
    /// Commits the staged request through <paramref name="submitGroupDecision"/> (command façade only).
    /// </summary>
    public static CoordinationAssistedWithdrawRedeployConfirmResult Confirm(
        Func<string, CoordinationDecision, string> submitGroupDecision)
    {
        if (_pending is null)
        {
            return new CoordinationAssistedWithdrawRedeployConfirmResult(false, "NO_PENDING_CONFIRM");
        }

        if (submitGroupDecision is null)
        {
            return new CoordinationAssistedWithdrawRedeployConfirmResult(false, "NO_SUBMIT_DELEGATE");
        }

        var pending = _pending;
        _pending = null;
        var status = submitGroupDecision(pending.GroupId, pending.Decision);
        var accepted = !status.StartsWith("Prevented:", StringComparison.Ordinal);
        return new CoordinationAssistedWithdrawRedeployConfirmResult(accepted, status);
    }

    private static CoordinationAssistedWithdrawRedeployChrome BuildChrome(
        CoordinationAssistedWithdrawRedeployPendingRequest pending)
    {
        if (pending.RedeploySuggestion)
        {
            return new CoordinationAssistedWithdrawRedeployChrome(
                Title: "Confirm withdraw / redeploy",
                Body: $"Assisted mode: confirm task group {pending.GroupId} withdraws and accepts redeploy mission suggestion. Cancel leaves the order log unchanged.",
                ConfirmLabel: "CONFIRM WITHDRAW / REDEPLOY",
                CancelLabel: "CANCEL");
        }

        return new CoordinationAssistedWithdrawRedeployChrome(
            Title: "Confirm withdraw",
            Body: $"Assisted mode: confirm task group {pending.GroupId} withdraw (RTB). Cancel leaves the order log unchanged.",
            ConfirmLabel: "CONFIRM WITHDRAW",
            CancelLabel: "CANCEL");
    }

    private static CoordinationAssistedWithdrawRedeployChrome EmptyChrome() =>
        new(string.Empty, string.Empty, string.Empty, string.Empty);
}

/// <summary>Staged assisted confirm request (presentation session state).</summary>
public sealed record CoordinationAssistedWithdrawRedeployPendingRequest(
    string GroupId,
    CoordinationDecision Decision,
    MissionIntentSnapshot? ReviewedIntent,
    bool RedeploySuggestion);
