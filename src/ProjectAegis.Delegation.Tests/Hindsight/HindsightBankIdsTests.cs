using ProjectAegis.Delegation.Hindsight;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Hindsight;

[TestFixture]
public sealed class HindsightBankIdsTests
{
    [Test]
    public void AgentDecision_normalizes_personality_and_agent_id()
    {
        Assert.That(
            HindsightBankIds.AgentDecision("EwSpecialist", "EW-02"),
            Is.EqualTo("agent-ewspecialist-ew-02"));
    }

    [Test]
    public void Aar_includes_scenario_and_run_slug()
    {
        Assert.That(
            HindsightBankIds.Aar("Baltic Replay", "run-001"),
            Is.EqualTo("aar-baltic-replay-run-001"));
    }

    [Test]
    public void AgentExperience_uses_agent_id_only()
    {
        Assert.That(HindsightBankIds.AgentExperience("swarm-coord-03"), Is.EqualTo("agent-xp-swarm-coord-03"));
    }
}
