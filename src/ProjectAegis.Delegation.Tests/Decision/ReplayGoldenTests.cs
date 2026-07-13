namespace ProjectAegis.Delegation.Tests.Decision;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

/// <summary>
/// Golden-replay regression for the deterministic simulation path — the in-code
/// equivalent of the <c>/replay-verify</c> gate.
/// </summary>
/// <remarks>
/// The agent-seeding golden (<see cref="AgentSeed_stream_matches_cross_process_golden"/>)
/// is the load-bearing test: its constants were computed independently of any process
/// run, so they only hold if the per-agent RNG salt is derived from a deterministic,
/// cross-process-stable hash. Reintroducing <c>string.GetHashCode</c> (DET-001) makes
/// the salt random per process and breaks these exact values in essentially every run.
/// </remarks>
[TestFixture]
public sealed class ReplayGoldenTests
{
    private const double RngDenominator = 0x1000000; // 2^24

    [Test]
    public void OrdinalHash_is_stable_golden()
    {
        // Computed independently of any process run. If string.GetHashCode is
        // reintroduced this constant cannot match across processes.
        Assert.That(DeterministicHash.OrdinalHash("a1"), Is.EqualTo(1012613629));
    }

    [Test]
    public void AgentSeed_stream_matches_cross_process_golden()
    {
        var salt = DeterministicHash.OrdinalHash("a1");
        var rng = new SeededRng(globalSeed: 1234, agentSalt: salt);

        // Golden numerators are exact dyadic rationals over 2^24 (k / 2^24 is
        // representable as a double with no rounding), so equality is exact.
        Assert.That(rng.NextUnit(), Is.EqualTo(4611399 / RngDenominator));
        Assert.That(rng.NextUnit(), Is.EqualTo(6250557 / RngDenominator));
        Assert.That(rng.NextUnit(), Is.EqualTo(6582717 / RngDenominator));
    }

    [Test]
    public void Replay_reproduces_identical_decision_log_across_fresh_runs()
    {
        var a = RunAndFingerprint(globalSeed: 4242);
        var b = RunAndFingerprint(globalSeed: 4242);

        // Full decision signature must match, not just the executed order kinds:
        // sim time, chosen kind, and the RNG draw that produced each decision.
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.Empty, "scenario should produce at least one decision");
    }

    [Test]
    public void Replay_diverges_when_seed_differs()
    {
        // Sanity check that the fingerprint is actually sensitive to the seed —
        // a fingerprint that never changed would make the reproduction test vacuous.
        var a = RunAndFingerprint(globalSeed: 1);
        var b = RunAndFingerprint(globalSeed: 999983);
        Assert.That(a, Is.Not.EqualTo(b));
    }

    /// <summary>
    /// Runs a fixed multi-tick, multi-agent scenario and returns a deterministic
    /// fingerprint of its decision log (the replay artifact being compared).
    /// </summary>
    private static IReadOnlyList<string> RunAndFingerprint(int globalSeed)
    {
        var orchestrator = new DelegationOrchestrator(globalSeed);

        foreach (var name in new[] { "a1", "a2", "a3" })
        {
            var unit = new UnitTarget(new TargetId($"u-{name}"));
            var agent = orchestrator.CreateAgent(
                new AgentId(name),
                PersonalityCatalog.All[0].Traits,
                AutonomyLevel.FullAutonomous);
            unit.Slot.SetActive(agent);
            orchestrator.Register(unit);
        }

        orchestrator.BeginExecution();

        // Advance with a varying contact picture
        for (var tick = 0; tick < 8; tick++)
        {
            var state = new ObservedState(
                tick,
                2 + (tick % 3),
                tick % 2,
                new Dictionary<TargetId, bool>(),
                PrimaryHostileDestroyed: false);
            orchestrator.Tick(state);
        }

        return orchestrator.DecisionLog.Records
            .Select(r => FormattableString.Invariant(
                $"{r.SimTime:R}|{r.AgentId.Value}|{r.ChosenKind}|{r.RngDraw:R}"))
            .ToArray();
    }
}
