using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Replay;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

public sealed class OrderLogReplayFingerprintSha256Tests
{
    [Test]
    public void ComputeSha256Hex_is_stable_for_same_log()
    {
        var log = new DecisionLog();
        log.AppendModeChange(new ModeChangeRecord(0, 0, 0, "Planning", "Executing"));

        var a = OrderLogReplayFingerprint.ComputeSha256Hex(log);
        var b = OrderLogReplayFingerprint.ComputeSha256Hex(log);
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Has.Length.EqualTo(64));
    }

    [Test]
    public void ComputeSha256Hex_differs_when_log_differs()
    {
        var left = new DecisionLog();
        left.AppendModeChange(new ModeChangeRecord(0, 0, 0, "Planning", "Executing"));
        var right = new DecisionLog();
        right.AppendModeChange(new ModeChangeRecord(0, 0, 0, "Planning", "Paused"));

        Assert.That(
            OrderLogReplayFingerprint.ComputeSha256Hex(left),
            Is.Not.EqualTo(OrderLogReplayFingerprint.ComputeSha256Hex(right)));
    }
}