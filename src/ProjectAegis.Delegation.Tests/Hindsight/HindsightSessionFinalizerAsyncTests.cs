using System.Net.Http;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Hindsight;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Tests.Helpers;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Trust;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.Tests.Hindsight;

[TestFixture]
public sealed class HindsightSessionFinalizerAsyncTests
{
    [Test]
    public async Task OnScenarioFinalizedAsync_retains_and_reflects_returning_Succeeded()
    {
        var client = new StubHindsightMemoryClient
        {
            ReflectResponse = "Agent a1 maintained ROE discipline.",
        };
        var options = new HindsightOptions
        {
            Enabled = true,
            ScenarioSlug = "baltic",
            RunId = "run-001",
            AarReflectQuery = "Summarize ROE performance",
        };
        var finalizer = new HindsightSessionFinalizer(client, options);
        var log = new DecisionLog();
        var signals = new[] { new TrustSignal("a1", "roe_compliance", 1.0) };

        var result = await finalizer.OnScenarioFinalizedAsync(log, signals, missionSucceeded: true, objectivesMetRatio: 1.0);

        Assert.That(result.Status, Is.EqualTo(HindsightFinalizeStatus.Succeeded));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.AarBankId, Is.EqualTo("aar-baltic-run-001"));
        Assert.That(result.ReflectionText, Is.EqualTo("Agent a1 maintained ROE discipline."));
        Assert.That(result.Error, Is.Null);
        Assert.That(client.RetainAsyncCalls.Select(c => c.BankId), Is.EqualTo(new[]
        {
            "aar-baltic-run-001",
            "agent-xp-a1",
        }));
    }

    [Test]
    public async Task OnScenarioFinalizedAsync_when_reflect_throws_HttpRequestException_returns_Unavailable_without_mutating_log()
    {
        var client = new StubHindsightMemoryClient
        {
            ReflectException = new HttpRequestException("Connection refused"),
        };
        var options = new HindsightOptions
        {
            Enabled = true,
            ScenarioSlug = "baltic",
            RunId = "run-002",
            AarReflectQuery = "Summarize AAR",
        };
        var finalizer = new HindsightSessionFinalizer(client, options);
        var log = new DecisionLog();
        log.AppendModeChange(new ModeChangeRecord(0, 1.0, 1, "Planning", "Executing"));
        var beforeFingerprint = log.ComputeFingerprint();

        var result = await finalizer.OnScenarioFinalizedAsync(log, Array.Empty<TrustSignal>(), false, 0.25);

        Assert.That(result.Status, Is.EqualTo(HindsightFinalizeStatus.Unavailable));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.AarBankId, Is.EqualTo("aar-baltic-run-002"));
        Assert.That(result.ReflectionText, Is.Null);
        Assert.That(result.Error, Does.Contain("Connection refused"));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(beforeFingerprint));
    }

    [Test]
    public async Task OnScenarioFinalizedAsync_when_retain_throws_HttpRequestException_returns_Unavailable()
    {
        var client = new StubHindsightMemoryClient
        {
            RetainException = new HttpRequestException("Sidecar down"),
        };
        var options = new HindsightOptions
        {
            Enabled = true,
            ScenarioSlug = "baltic",
            RunId = "run-003",
            AarReflectQuery = "Summarize AAR",
        };
        var finalizer = new HindsightSessionFinalizer(client, options);

        var result = await finalizer.OnScenarioFinalizedAsync(
            new DecisionLog(), Array.Empty<TrustSignal>(), true, 1.0);

        Assert.That(result.Status, Is.EqualTo(HindsightFinalizeStatus.Unavailable));
        Assert.That(result.Error, Does.Contain("Sidecar down"));
    }

    [Test]
    public async Task OnScenarioFinalizedAsync_when_reflect_is_cancelled_or_timed_out_returns_Unavailable()
    {
        var client = new StubHindsightMemoryClient
        {
            ReflectException = new TaskCanceledException("Request timed out"),
        };
        var options = new HindsightOptions
        {
            Enabled = true,
            ScenarioSlug = "baltic",
            RunId = "run-004",
            AarReflectQuery = "Summarize AAR",
        };
        var finalizer = new HindsightSessionFinalizer(client, options);

        var result = await finalizer.OnScenarioFinalizedAsync(
            new DecisionLog(), Array.Empty<TrustSignal>(), true, 1.0);

        Assert.That(result.Status, Is.EqualTo(HindsightFinalizeStatus.Unavailable));
        Assert.That(result.Error, Does.Contain("Request timed out"));
    }

    [Test]
    public async Task OnScenarioFinalizedAsync_when_reflect_returns_null_returns_Unavailable()
    {
        var client = new StubHindsightMemoryClient
        {
            ReflectResponse = null,
        };
        var options = new HindsightOptions
        {
            Enabled = true,
            ScenarioSlug = "baltic",
            RunId = "run-005",
            AarReflectQuery = "Summarize AAR",
        };
        var finalizer = new HindsightSessionFinalizer(client, options);

        var result = await finalizer.OnScenarioFinalizedAsync(
            new DecisionLog(), Array.Empty<TrustSignal>(), true, 1.0);

        Assert.That(result.Status, Is.EqualTo(HindsightFinalizeStatus.Unavailable));
        Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task OnScenarioFinalizedAsync_returns_Disabled_or_Skipped_when_disabled_or_no_query()
    {
        var client = new StubHindsightMemoryClient();
        var log = new DecisionLog();

        var disabled = new HindsightSessionFinalizer(client, new HindsightOptions { Enabled = false });
        var disabledResult = await disabled.OnScenarioFinalizedAsync(log, Array.Empty<TrustSignal>(), true, 1.0);
        Assert.That(disabledResult.Status, Is.EqualTo(HindsightFinalizeStatus.Disabled));
        Assert.That(client.RetainAsyncCalls, Is.Empty);

        var skipped = new HindsightSessionFinalizer(client, new HindsightOptions
        {
            Enabled = true,
            ScenarioSlug = "baltic",
            RunId = "run-006",
            AarReflectQuery = null,
        });
        var skippedResult = await skipped.OnScenarioFinalizedAsync(log, Array.Empty<TrustSignal>(), true, 1.0);
        Assert.That(skippedResult.Status, Is.EqualTo(HindsightFinalizeStatus.Skipped));
        Assert.That(client.RetainAsyncCalls, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Order_log_fingerprint_is_identical_with_hindsight_off_on_or_unreachable()
    {
        static string RunOrchestrator(HindsightOptions? hindsightOptions)
        {
            var orchestrator = new DelegationOrchestrator(99, policyEvaluator: null, hindsight: hindsightOptions);
            var unit = new UnitTarget(new TargetId("u1"));
            var agent = orchestrator.CreateAgent(
                new AgentId("a1"),
                PersonalityCatalog.All[0].Traits,
                AutonomyLevel.FullAutonomous,
                policy: new EngageOnlyPolicy());
            orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
            orchestrator.Register(unit);
            orchestrator.BeginExecution();
            for (var t = 0; t < 4; t++)
            {
                orchestrator.Tick(MvpObservedStates.EngageTick(t));
            }

            return orchestrator.DecisionLog.ComputeFingerprint();
        }

        var fpOff = RunOrchestrator(null);
        // Unreachable local port (nothing listening on port 19888)
        var unreachableOptions = new HindsightOptions
        {
            Enabled = true,
            BaseUrl = "http://127.0.0.1:19888",
            ScenarioSlug = "baltic",
            RunId = "det-test",
            AarReflectQuery = "Check AAR",
        };
        var fpUnreachable = RunOrchestrator(unreachableOptions);

        Assert.That(fpUnreachable, Is.EqualTo(fpOff));

        var integration = HindsightIntegration.TryCreate(unreachableOptions);
        Assert.That(integration, Is.Not.Null);
        var log = new DecisionLog();
        var before = log.ComputeFingerprint();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var result = await integration!.OnScenarioFinalizedAsync(log, Array.Empty<TrustSignal>(), true, 1.0, cts.Token);
        Assert.That(result.Status, Is.EqualTo(HindsightFinalizeStatus.Unavailable));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(before));
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits) =>
            [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
    }

    private sealed class StubHindsightMemoryClient : IHindsightMemoryClient
    {
        public List<(string BankId, string Content)> RetainAsyncCalls { get; } = new();
        public Exception? RetainException { get; init; }
        public Exception? ReflectException { get; init; }
        public string? ReflectResponse { get; init; }

        public void RetainFireAndForget(string bankId, string content, string? context = null) =>
            RetainAsyncCalls.Add((bankId, content));

        public Task RetainAsync(string bankId, string content, CancellationToken cancellationToken = default)
        {
            if (RetainException is not null)
            {
                return Task.FromException(RetainException);
            }

            RetainAsyncCalls.Add((bankId, content));
            return Task.CompletedTask;
        }

        public Task<string?> ReflectAsync(string bankId, string query, CancellationToken cancellationToken = default)
        {
            if (ReflectException is not null)
            {
                return Task.FromException<string?>(ReflectException);
            }

            return Task.FromResult(ReflectResponse);
        }
    }
}
