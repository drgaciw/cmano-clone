namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;

[TestFixture]
public sealed class BalticReplayStepperTests
{
    [Test]
    public void Stepper_matches_BalticReplayHarness_golden_v2_byte_for_byte()
    {
        var expected = BalticReplayHarness.Run(seed: 42, scenarioPolicyId: "baltic-patrol", ticks: 4);
        Assert.That(expected.WorldHash, Is.EqualTo(17144800277401907079UL));

        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            var stepper = BalticReplayStepper.Create(seed: 42, scenarioPolicyId: "baltic-patrol");
            for (var t = 0; t < 4; t++)
            {
                stepper.StepOneObservedTick();
            }

            var actual = stepper.BuildResult(ticks: 4);
            Assert.That(actual.WorldHash, Is.EqualTo(expected.WorldHash));
            Assert.That(actual.DetectionWorldHash, Is.EqualTo(expected.DetectionWorldHash));
            Assert.That(actual.Fingerprint, Is.EqualTo(expected.Fingerprint));
            Assert.That(actual.FingerprintSha256, Is.EqualTo(expected.FingerprintSha256));
            Assert.That(actual.EngagementCount, Is.EqualTo(expected.EngagementCount));
            Assert.That(actual.ScoringCsvRow, Is.EqualTo(expected.ScoringCsvRow));
            Assert.That(actual.Checkpoints, Is.EqualTo(expected.Checkpoints));
            Assert.That(actual.FireOrder, Is.EqualTo(expected.FireOrder));
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }

    [Test]
    public void Stepper_matches_BalticReplayHarness_v3_patrol_byte_for_byte()
    {
        var expected = BalticReplayHarness.Run(seed: 42, scenarioPolicyId: "baltic-v3-patrol", ticks: 4);

        BalticV3SideRegistry.ClearScenarioSides();
        try
        {
            var stepper = BalticReplayStepper.Create(seed: 42, scenarioPolicyId: "baltic-v3-patrol");
            for (var t = 0; t < 4; t++)
            {
                stepper.StepOneObservedTick();
            }

            var actual = stepper.BuildResult(ticks: 4);
            Assert.That(actual.WorldHash, Is.EqualTo(expected.WorldHash));
            Assert.That(actual.DetectionWorldHash, Is.EqualTo(expected.DetectionWorldHash));
            Assert.That(actual.Fingerprint, Is.EqualTo(expected.Fingerprint));
            Assert.That(actual.FingerprintSha256, Is.EqualTo(expected.FingerprintSha256));
            Assert.That(actual.EngagementCount, Is.EqualTo(expected.EngagementCount));
            Assert.That(actual.Checkpoints, Is.EqualTo(expected.Checkpoints));
        }
        finally
        {
            BalticV3SideRegistry.ClearScenarioSides();
        }
    }
}
