namespace ProjectAegis.Data.Tests.Import;

using NUnit.Framework;
using ProjectAegis.Data.Import;

/// <summary>
/// UAT CLI-5: the import batch clock base tick derives from the markdown file name and must be
/// identical across processes (string.GetHashCode() is randomized per process).
/// </summary>
[TestFixture]
public sealed class ImportBatchClockDeterminismTests
{
    [TestCase("/tmp/sample-import.md", 3002910000L)]
    [TestCase("/tmp/x.md", 3464370000L)]
    public void Import_batch_clock_base_tick_is_process_stable(string path, long expectedFirstTick)
    {
        var clock = CmoMarkdownImportProposer.CreateImportBatchClock(path);
        Assert.That(clock.UtcTicks, Is.EqualTo(expectedFirstTick));
    }

    [TestCase("legacy-patrol-ship", 0x1747d5ae)]
    [TestCase("", unchecked((int)2166136261u))]
    public void StableHash_Fnv1a32_matches_pinned_literals(string input, int expected)
    {
        Assert.That(StableHash.Fnv1a32(input), Is.EqualTo(expected));
    }
}
