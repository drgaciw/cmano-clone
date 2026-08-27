namespace ProjectAegis.Delegation.Tests.Skills;

using ProjectAegis.Delegation.Skills;
using NUnit.Framework;

/// <summary>DRG-196 / AGC-01: named, discoverable, capability-scoped Slice A skills.</summary>
[TestFixture]
public sealed class SkillCatalogTests
{
    [Test]
    public void SliceA_names_track_datalink_pairing_and_explanation()
    {
        var ids = SkillCatalog.SliceA.Select(s => s.SkillId).ToArray();
        Assert.That(ids, Is.EqualTo(new[]
        {
            SkillIds.TrackAssess,
            SkillIds.DatalinkReason,
            SkillIds.PairingRecommend,
            SkillIds.Explain,
        }));
    }

    [Test]
    public void SliceA_skills_do_not_list_submit_lane()
    {
        Assert.That(SkillCatalog.SliceA.SelectMany(s => s.Lanes), Does.Not.Contain(SkillLane.Submit));
    }

    [Test]
    public void SubmitVerb_is_host_only()
    {
        Assert.That(SkillCatalog.SubmitVerbId, Is.EqualTo(SkillIds.Submit));
        Assert.That(SkillCatalog.TryGet(SkillIds.Submit, out _), Is.False);
    }

    [Test]
    public void TryGet_unknown_skill_is_hard_miss()
    {
        Assert.That(SkillCatalog.TryGet("c2.invented.fire", out _), Is.False);
    }
}
