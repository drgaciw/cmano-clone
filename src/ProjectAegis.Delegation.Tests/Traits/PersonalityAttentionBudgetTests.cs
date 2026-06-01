namespace ProjectAegis.Delegation.Tests.Traits;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class PersonalityAttentionBudgetTests
{
    [Test]
    public void SwarmCoordinator_gets_25_percent_attention_budget_boost()
    {
        var preset = PersonalityCatalog.All.First(p => p.Name == "SwarmCoordinator");
        Assert.That(PersonalityCatalog.ResolveAttentionBudget(preset), Is.EqualTo(25.0));
    }

    [Test]
    public void CreateAgentFromPreset_applies_personality_budget()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var preset = PersonalityCatalog.All.First(p => p.Name == "EwSpecialist");
        var agent = orchestrator.CreateAgentFromPreset(
            new AgentId("ew"),
            preset,
            AutonomyLevel.FullAutonomous);

        Assert.That(agent.AttentionBudget, Is.EqualTo(18.0));
    }
}
