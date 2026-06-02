using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Hindsight;
using ProjectAegis.Delegation.Trust;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Hindsight;

[TestFixture]
public sealed class HindsightSessionFinalizerTests
{
    [Test]
    public void OnScenarioFinalized_retains_aar_and_experience_banks()
    {
        var sink = new RecordingHindsightMemoryClient();
        var options = new HindsightOptions
        {
            Enabled = true,
            ScenarioSlug = "baltic",
            RunId = "run-001",
        };
        var finalizer = new HindsightSessionFinalizer(sink, options);
        var log = new DecisionLog();
        var signals = new[] { new TrustSignal("ew-02", "roe_violations", 2) };

        finalizer.OnScenarioFinalized(log, signals, missionSucceeded: false, objectivesMetRatio: 0.5);

        Assert.That(sink.RetainCalls, Has.Count.EqualTo(2));
        Assert.That(sink.RetainCalls[0].BankId, Is.EqualTo("aar-baltic-run-001"));
        Assert.That(sink.RetainCalls[1].BankId, Is.EqualTo("agent-xp-ew-02"));
    }

    private sealed class RecordingHindsightMemoryClient : IHindsightMemoryClient
    {
        public List<(string BankId, string Content)> RetainCalls { get; } = new();

        public void RetainFireAndForget(string bankId, string content, string? context = null) =>
            RetainCalls.Add((bankId, content));

        public Task RetainAsync(string bankId, string content, CancellationToken cancellationToken = default)
        {
            RetainCalls.Add((bankId, content));
            return Task.CompletedTask;
        }

        public Task<string?> ReflectAsync(
            string bankId,
            string query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }
}
