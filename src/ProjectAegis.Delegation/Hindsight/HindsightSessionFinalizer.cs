namespace ProjectAegis.Delegation.Hindsight;

using System.Globalization;
using System.Text;
using Decision;
using Trust;

public sealed class HindsightSessionFinalizer
{
    private readonly IHindsightMemoryClient _client;
    private readonly HindsightOptions _options;

    public HindsightSessionFinalizer(IHindsightMemoryClient client, HindsightOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <summary>
    /// ADR-019: takes <see cref="IReadOnlyOrderLog"/>, not the concrete log — the
    /// read-only guarantee for AAR is enforced by the type, not by convention.
    /// </summary>
    public void OnScenarioFinalized(
        IReadOnlyOrderLog log,
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

    /// <summary>
    /// DRG-746: awaitable session finalization beside <see cref="OnScenarioFinalized"/>.
    /// Retains AAR/XP and awaits reflection, returning an explicit <see cref="HindsightFinalizeResult"/>
    /// (with <see cref="HindsightFinalizeStatus.Unavailable"/> when the sidecar is unreachable)
    /// without throwing or mutating <see cref="IReadOnlyOrderLog"/>.
    /// </summary>
    public async Task<HindsightFinalizeResult> OnScenarioFinalizedAsync(
        IReadOnlyOrderLog log,
        IReadOnlyList<TrustSignal> trustSignals,
        bool missionSucceeded,
        double objectivesMetRatio,
        CancellationToken cancellationToken = default)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        if (trustSignals is null)
        {
            throw new ArgumentNullException(nameof(trustSignals));
        }

        if (!_options.Enabled)
        {
            return new HindsightFinalizeResult(
                HindsightFinalizeStatus.Disabled,
                AarBankId: null,
                ReflectionText: null);
        }

        var runId = _options.RunId ?? TruncateFingerprint(log.ComputeFingerprint());
        var aarBank = _options.FinalizeAarBank
            ? HindsightBankIds.Aar(_options.ScenarioSlug, runId)
            : null;

        try
        {
            if (_options.FinalizeAarBank && aarBank is not null)
            {
                await _client.RetainAsync(
                    aarBank,
                    BuildAarSummary(log, missionSucceeded, objectivesMetRatio),
                    cancellationToken).ConfigureAwait(false);
            }

            if (_options.FinalizeCampaignExperience)
            {
                foreach (var signal in trustSignals)
                {
                    var xpBank = HindsightBankIds.AgentExperience(signal.AgentId);
                    var content =
                        $"trust_metric={signal.Metric} value={signal.Value.ToString(CultureInfo.InvariantCulture)} mission_succeeded={missionSucceeded} objectives_met_ratio={objectivesMetRatio.ToString(CultureInfo.InvariantCulture)}";
                    await _client.RetainAsync(xpBank, content, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            return new HindsightFinalizeResult(
                HindsightFinalizeStatus.Unavailable,
                aarBank,
                ReflectionText: null,
                Error: $"{ex.GetType().Name}: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(_options.AarReflectQuery) || !_options.FinalizeAarBank || aarBank is null)
        {
            return new HindsightFinalizeResult(
                HindsightFinalizeStatus.Skipped,
                aarBank,
                ReflectionText: null);
        }

        try
        {
            var reflection = await _client.ReflectAsync(aarBank, _options.AarReflectQuery!, cancellationToken)
                .ConfigureAwait(false);
            if (reflection is null)
            {
                return new HindsightFinalizeResult(
                    HindsightFinalizeStatus.Unavailable,
                    aarBank,
                    ReflectionText: null,
                    Error: "ReflectAsync returned null.");
            }

            return new HindsightFinalizeResult(
                HindsightFinalizeStatus.Succeeded,
                aarBank,
                ReflectionText: reflection);
        }
        catch (Exception ex)
        {
            return new HindsightFinalizeResult(
                HindsightFinalizeStatus.Unavailable,
                aarBank,
                ReflectionText: null,
                Error: $"{ex.GetType().Name}: {ex.Message}");
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
        IReadOnlyOrderLog log,
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

        var rejoinMerges = 0;
        foreach (var entry in log.ChronologicalEntries())
        {
            if (entry.Kind == OrderLogEntryKind.CommsGridRejoin
                && entry.Payload is CommsGridRejoinRecord rejoin)
            {
                rejoinMerges += rejoin.InformativeMerges.Count;
            }
        }

        if (rejoinMerges > 0)
        {
            builder.Append($" comms_rejoin_merges={rejoinMerges}.");
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
