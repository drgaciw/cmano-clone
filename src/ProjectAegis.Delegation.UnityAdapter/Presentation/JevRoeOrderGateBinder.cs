using System.Globalization;
using System.Text.Json;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Fail-closed ROE Noul gate state for Jev VS3 suggested orders (DRG-321 / ADR-010).</summary>
public sealed record JevSuggestedOrderGateState(
    bool CardVisible,
    bool ConfirmEnabled,
    bool IsAutoFire,
    string GroupId,
    CoordinationDecision Decision,
    double? RoeNoul,
    string? RiskLevel,
    string ModelPin,
    string OpenRouterModel,
    string SummaryLine,
    string? BlockReason)
{
    public static JevSuggestedOrderGateState Empty { get; } = Closed("NO_SUGGESTED_ORDER");

    public static JevSuggestedOrderGateState Closed(
        string reasonCode,
        double? roeNoul = null,
        string? riskLevel = null) =>
        new(
            CardVisible: false,
            ConfirmEnabled: false,
            IsAutoFire: false,
            GroupId: string.Empty,
            Decision: CoordinationDecision.Hold,
            RoeNoul: roeNoul,
            RiskLevel: riskLevel,
            ModelPin: JevDisclosurePresentation.PinnedModel,
            OpenRouterModel: JevDisclosurePresentation.PinnedOpenRouterModel,
            SummaryLine: $"JEV ROE GATE CLOSED: {reasonCode}",
            BlockReason: reasonCode);
}

/// <summary>
/// Evaluates <c>aegis.jev.vs3.state.v0</c> ROE Noul payloads for <c>#pending-jev-suggested-order</c> (DRG-321).
/// Fails closed on offline, unpinned model, <c>roeNoul &lt; DefaultRoeNoulFloor</c>, <c>suggest_ok_under_roe != yes</c>,
/// high/blocked risk, or advisory-only <see cref="CoordinationDecision.Reattack"/>. Never auto-fires.
/// </summary>
public static class JevRoeOrderGateBinder
{
    public const string StateSchema = "aegis.jev.vs3.state.v0";
    public const double DefaultRoeNoulFloor = 0.80;

    public const string ReasonJevOffline = "JEV_OFFLINE";
    public const string ReasonUnpinnedModel = "UNPINNED_MODEL";
    public const string ReasonRoeNoulBelowFloor = "ROE_NOUL_BELOW_FLOOR";
    public const string ReasonRoeSuggestDenied = "ROE_SUGGEST_DENIED";
    public const string ReasonRiskBlocked = "ORDER_PROPOSAL_RISK_BLOCKED";
    public const string ReasonAdvisoryDecisionOnly = "ADVISORY_DECISION_ONLY";
    public const string ReasonInvalidProposal = "INVALID_PROPOSAL";

    public static JevSuggestedOrderGateState Evaluate(
        string? json,
        double roeNoulFloor = DefaultRoeNoulFloor)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return JevSuggestedOrderGateState.Closed(ReasonInvalidProposal);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json!);
        }
        catch (JsonException)
        {
            return JevSuggestedOrderGateState.Closed(ReasonInvalidProposal);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return JevSuggestedOrderGateState.Closed(ReasonInvalidProposal);
            }

            var schema = OptionalString(root, "schema");
            if (!string.Equals(schema, StateSchema, StringComparison.Ordinal))
            {
                return JevSuggestedOrderGateState.Closed(ReasonInvalidProposal);
            }

            RejectPrivatePayload(root);

            if (ReadJevDecide(root) != true)
            {
                return JevSuggestedOrderGateState.Closed(ReasonJevOffline);
            }

            if (!root.TryGetProperty("jevDecision", out var jevDecision) || jevDecision.ValueKind != JsonValueKind.Object)
            {
                return JevSuggestedOrderGateState.Closed(ReasonJevOffline);
            }

            var model = OptionalString(jevDecision, "model");
            if (!string.Equals(model, JevDisclosurePresentation.PinnedOpenRouterModel, StringComparison.Ordinal)
                && !string.Equals(model, JevDisclosurePresentation.PinnedModel, StringComparison.Ordinal))
            {
                return JevSuggestedOrderGateState.Closed(ReasonUnpinnedModel);
            }

            var roeNoul = OptionalDouble(jevDecision, "roeNoul");
            var riskLevel = OptionalString(jevDecision, "order_proposal_risk");
            var suggestOk = OptionalString(jevDecision, "suggest_ok_under_roe");

            if (!string.Equals(suggestOk, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return JevSuggestedOrderGateState.Closed(ReasonRoeSuggestDenied, roeNoul, riskLevel);
            }

            var floor = double.IsNaN(roeNoulFloor) || double.IsInfinity(roeNoulFloor)
                ? DefaultRoeNoulFloor
                : Math.Clamp(roeNoulFloor, 0d, 1d);
            if (!roeNoul.HasValue || roeNoul.Value < floor)
            {
                return JevSuggestedOrderGateState.Closed(ReasonRoeNoulBelowFloor, roeNoul, riskLevel);
            }

            if (!IsRiskPermitted(riskLevel))
            {
                return JevSuggestedOrderGateState.Closed(ReasonRiskBlocked, roeNoul, riskLevel);
            }

            if (!root.TryGetProperty("proposal", out var proposal) || proposal.ValueKind != JsonValueKind.Object)
            {
                return JevSuggestedOrderGateState.Closed(ReasonInvalidProposal, roeNoul, riskLevel);
            }

            var groupId = OptionalString(proposal, "groupId");
            var decisionRaw = OptionalString(proposal, "decision");
            if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(decisionRaw))
            {
                return JevSuggestedOrderGateState.Closed(ReasonInvalidProposal, roeNoul, riskLevel);
            }

            var trimmedDecision = decisionRaw!.Trim();
            var isAlphaOnly = trimmedDecision.Length > 0;
            for (var i = 0; i < trimmedDecision.Length; i++)
            {
                if (!char.IsLetter(trimmedDecision[i]))
                {
                    isAlphaOnly = false;
                    break;
                }
            }

            if (!isAlphaOnly
                || !Enum.TryParse<CoordinationDecision>(trimmedDecision, ignoreCase: true, out var decision)
                || !Enum.IsDefined(typeof(CoordinationDecision), decision))
            {
                return JevSuggestedOrderGateState.Closed(ReasonInvalidProposal, roeNoul, riskLevel);
            }

            if (decision == CoordinationDecision.Reattack)
            {
                return JevSuggestedOrderGateState.Closed(ReasonAdvisoryDecisionOnly, roeNoul, riskLevel);
            }

            if (decision != CoordinationDecision.Hold && decision != CoordinationDecision.Withdraw)
            {
                return JevSuggestedOrderGateState.Closed(ReasonInvalidProposal, roeNoul, riskLevel);
            }

            var normalizedRisk = riskLevel!.Trim().ToLowerInvariant();
            var summary = string.Format(
                CultureInfo.InvariantCulture,
                "JEV VS3 ({0}) | {1} -> {2} | roeNoul={3:F2} | risk={4}",
                JevDisclosurePresentation.PinnedModel,
                groupId,
                decision,
                roeNoul.Value,
                normalizedRisk);

            return new JevSuggestedOrderGateState(
                CardVisible: true,
                ConfirmEnabled: true,
                IsAutoFire: false,
                GroupId: groupId!,
                Decision: decision,
                RoeNoul: roeNoul.Value,
                RiskLevel: normalizedRisk,
                ModelPin: JevDisclosurePresentation.PinnedModel,
                OpenRouterModel: JevDisclosurePresentation.PinnedOpenRouterModel,
                SummaryLine: summary,
                BlockReason: null);
        }
    }

    private static bool IsRiskPermitted(string? riskLevel) =>
        string.Equals(riskLevel, "low", StringComparison.OrdinalIgnoreCase)
        || string.Equals(riskLevel, "medium", StringComparison.OrdinalIgnoreCase);

    private static bool? ReadJevDecide(JsonElement root)
    {
        if (!root.TryGetProperty("skillAvailability", out var skill) || skill.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!skill.TryGetProperty("jevDecide", out var flag))
        {
            return null;
        }

        return flag.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static void RejectPrivatePayload(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                RejectPrivatePayload(item);
            }

            return;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals("decisionLog", StringComparison.OrdinalIgnoreCase)
                || property.Name.Equals("scenarioXml", StringComparison.OrdinalIgnoreCase)
                || property.Name.Equals("secret", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Jev VS3 accepts projection fixtures only. Private scenario or log payloads are refused.");
            }

            RejectPrivatePayload(property.Value);
        }
    }

    private static string? OptionalString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static double? OptionalDouble(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number
            || !value.TryGetDouble(out var number)
            || double.IsNaN(number)
            || double.IsInfinity(number)
            || number < 0d
            || number > 1d)
        {
            return null;
        }

        return number;
    }
}
