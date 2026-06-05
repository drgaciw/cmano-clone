namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class BalticReplayHarnessKillPersistenceTests
{
    [Test]
    public void Kill_prevents_hostile_re_detection_on_later_ticks()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var lines = result.Fingerprint.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var detectAfterLost = false;
        var sawLost = false;
        foreach (var line in lines)
        {
            if (line.Contains("|Detected|Lost", StringComparison.Ordinal))
            {
                sawLost = true;
            }
            else if (sawLost &&
                     line.StartsWith("ContactChange|", StringComparison.Ordinal) &&
                     line.Contains("|Unknown|Detected", StringComparison.Ordinal))
            {
                detectAfterLost = true;
            }
        }

        Assert.That(sawLost, Is.True);
        Assert.That(detectAfterLost, Is.False);
    }
}