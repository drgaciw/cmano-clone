namespace ProjectAegis.Delegation.Tests.Skills;

using Delegation.Skills;
using NUnit.Framework;

[TestFixture]
public sealed class SkillEnvelopeValidatorReadGuardTests
{
    private static readonly SkillDescriptor ReadOnly = new("advice.test", "Test advice", [SkillLane.Read], []);

    private static SkillEnvelope Envelope(SkillLane lane = SkillLane.Read, string skillId = "advice.test", string? commandId = null) =>
        new(lane, skillId, "inv-1", null, 1, 1.0, commandId, null, null, RequiredApproval.None,
            [], [], "rationale", null, null,
            new ReplayProvenance(skillId, "inv-1", 1, 1.0, null, null, false));

    [Test]
    public void Read_envelope_without_command_is_accepted()
    {
        var result = SkillEnvelopeValidator.ValidateRead(Envelope(), ReadOnly);
        Assert.That(result.Ok, Is.True);
    }

    [Test]
    public void Read_envelope_naming_a_command_is_rejected()
    {
        var result = SkillEnvelopeValidator.ValidateRead(Envelope(commandId: "engage"), ReadOnly);
        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonReadMustNotCommand));
    }

    [Test]
    public void Non_read_lane_is_rejected()
    {
        var result = SkillEnvelopeValidator.ValidateRead(Envelope(lane: SkillLane.Propose), ReadOnly);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonLaneNotAllowed));
    }

    [Test]
    public void Mismatched_skill_id_is_rejected()
    {
        var result = SkillEnvelopeValidator.ValidateRead(Envelope(skillId: "other"), ReadOnly);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonUnknownSkill));
    }
}
