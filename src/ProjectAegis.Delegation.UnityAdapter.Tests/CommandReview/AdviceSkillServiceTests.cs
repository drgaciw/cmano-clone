using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

[TestFixture]
public sealed class AdviceSkillServiceTests
{
    [TestCase(AdviceSkillIds.DatalinkAssessment)]
    [TestCase(AdviceSkillIds.ResourceRecommendation)]
    [TestCase(AdviceSkillIds.MissionPackageExplanation)]
    public void Named_advisory_skill_is_discoverable_and_callable(string skillId)
    {
        Assert.That(AdviceSkillCatalog.TryGet(skillId, out var descriptor), Is.True);
        var result = AdviceSkillService.Invoke(skillId, AdviceFrame.Unavailable(1, 1, AdviceAvailability.EvidenceUnavailable, "fallback"));
        Assert.That(result.Skill.SkillId, Is.EqualTo(skillId));
        Assert.That(result.Skill.Lane.ToString(), Is.EqualTo("Read"));
        Assert.That(result.Advice.IsFireOrder, Is.False);
        Assert.That(descriptor.CommandIds, Is.Empty);
    }

    [Test]
    public void Fractional_sim_time_distinguishes_invocations_within_one_tick()
    {
        var first = AdviceSkillService.Invoke(AdviceSkillIds.DatalinkAssessment,
            AdviceFrame.Unavailable(1, 1.1, AdviceAvailability.EvidenceUnavailable, "fallback"));
        var second = AdviceSkillService.Invoke(AdviceSkillIds.DatalinkAssessment,
            AdviceFrame.Unavailable(1, 1.2, AdviceAvailability.EvidenceUnavailable, "fallback"));
        Assert.That(first.Skill.InvocationId, Is.Not.EqualTo(second.Skill.InvocationId));
    }
}
