using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Hindsight;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Hindsight;

[TestFixture]
public sealed class HindsightOrderLogHookTests
{
    [Test]
    public void OnAppended_retain_agent_decisions_only()
    {
        var sink = new RecordingHindsightMemoryClient();
        var hook = new HindsightOrderLogHook(sink);
        hook.RegisterAgent(new AgentId("a1"), "Aggressive");

        var payload = new AgentDecisionPayload(
            1,
            1.0,
            new AgentId("a1"),
            new TargetId("t1"),
            AutonomyLevel.Assisted,
            OrderKind.Move,
            [new ScoredIntent(OrderKind.Move, 1, RiskLevel.Low)],
            "move",
            1,
            10,
            0.1);

        hook.OnAppended(new OrderLogEntry(1, OrderLogEntryKind.AgentDecision, 1.0, payload));
        hook.OnAppended(new OrderLogEntry(
            2,
            OrderLogEntryKind.PolicyDenial,
            1.0,
            new PolicyDenialRecord(
                2,
                1.0,
                1,
                new AgentId("a1"),
                new TargetId("t1"),
                0,
                FireAbortReason.RoeHoldFire,
                OrderKind.Engage)));

        Assert.That(sink.RetainCalls, Has.Count.EqualTo(1));
        Assert.That(sink.RetainCalls[0].BankId, Is.EqualTo("agent-aggressive-a1"));
        Assert.That(sink.RetainCalls[0].Content, Does.Contain("Chose Move"));
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
