namespace ProjectAegis.Delegation.Hindsight;

/// <summary>Wires Hindsight retain on order-log append and session finalize (Phase A/B).</summary>
public sealed class HindsightIntegration
{
    private readonly HindsightSessionFinalizer _finalizer;

    private HindsightIntegration(
        IHindsightOrderLogHook orderLogHook,
        HindsightSessionFinalizer finalizer,
        IHindsightMemoryClient client)
    {
        OrderLogHook = orderLogHook;
        _finalizer = finalizer;
        Client = client;
    }

    public IHindsightOrderLogHook OrderLogHook { get; }

    public IHindsightMemoryClient Client { get; }

    public static HindsightIntegration? TryCreate(HindsightOptions? options)
    {
        if (options is null || !options.Enabled)
        {
            return null;
        }

        var http = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        var client = new HindsightHttpMemoryClient(http, options.ApiKey);
        IHindsightOrderLogHook hook = options.RetainAgentDecisions
            ? new HindsightOrderLogHook(client)
            : NullHindsightOrderLogHook.Instance;
        var finalizer = new HindsightSessionFinalizer(client, options);
        return new HindsightIntegration(hook, finalizer, client);
    }

    public void OnScenarioFinalized(
        Decision.DecisionLog log,
        IReadOnlyList<Trust.TrustSignal> trustSignals,
        bool missionSucceeded,
        double objectivesMetRatio) =>
        _finalizer.OnScenarioFinalized(log, trustSignals, missionSucceeded, objectivesMetRatio);
}
