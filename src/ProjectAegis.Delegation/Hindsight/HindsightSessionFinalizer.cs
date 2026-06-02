namespace ProjectAegis.Delegation.Hindsight;

using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Trust;

public sealed class HindsightSessionFinalizer
{
    private readonly IHindsightMemoryClient _client;
    private readonly HindsightOptions _options;

    public HindsightSessionFinalizer(IHindsightMemoryClient client, HindsightOptions options)
    {
        _client = client;
        _options = options;
    }

    public void OnScenarioFinalized(
        DecisionLog log,
        IReadOnlyList<TrustSignal> trustSignals,
        bool missionSucceeded,
        double objectivesMetRatio)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var runId = _options.RunId ?? TruncateFingerprint(log.ComputeFingerprint());
        if (_options.FinalizeAarBank)
        {
            var aarBank = HindsightBankIds.Aar(_options.ScenarioSlug, runId);
            _client.RetainFireAndForget(aarBank, BuildAarSummary(log, missionSucceeded, objectivesMetRatio));
        }

        if (_options.FinalizeCampaignExperience)
        {
            foreach (var signal in trustSignals)
            {
                var xpBank = HindsightBankIds.AgentExperience(signal.AgentId);
                var content =
                    $"trust_metric={signal.Metric} value={signal.Value.ToString(CultureInfo.InvariantCulture)} mission_succeeded={missionSucceeded} objectives_met_ratio={objectivesMetRatio.ToString(CultureInfo.InvariantCulture)}";
                _client.RetainFireAndForget(xpBank, content, context: "campaign-trust");
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.AarReflectQuery) && _options.FinalizeAarBank)
        {
            var aarBank = HindsightBankIds.Aar(_options.ScenarioSlug, runId);
            _ = ReflectAarAsync(aarBank, _options.AarReflectQuery!);
        }
    }

    private async Task ReflectAarAsync(string bankId, string query)
    {
        try
        {
            await _client.ReflectAsync(bankId, query).ConfigureAwait(false);
        }
        catch
        {
            // Sidecar is optional; reflection failures must not affect sim teardown.
        }
    }

    private static string BuildAarSummary(
        DecisionLog log,
        bool missionSucceeded,
        double objectivesMetRatio)
    {
        var builder = new StringBuilder();
        builder.Append($"mission_succeeded={missionSucceeded} objectives_met_ratio={objectivesMetRatio.ToString(CultureInfo.InvariantCulture)}. ");
        builder.Append($"decisions={log.Records.Count} policy_denials={log.PolicyDenials.Count} engagements={log.Engagements.Count}. ");
        builder.Append($"order_log_fingerprint={log.ComputeFingerprint()}. ");

        foreach (var group in log.Records.GroupBy(r => r.AgentId.Value).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var overrides = log.ControllerChanges.Count(c =>
                c.NewKind == "Human" && c.AgentId?.Value == group.Key);
            builder.Append($" Agent {group.Key}: {group.Count()} decisions, {overrides} player overrides.");
        }

        return builder.ToString();
    }

    private static string TruncateFingerprint(string fingerprint)
    {
        if (string.IsNullOrEmpty(fingerprint))
        {
            return "run-unknown";
        }

        return fingerprint.Length <= 16 ? fingerprint : fingerprint[..16];
    }
}
