namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

internal static class ReplayGoldenAssertions
{
    public static void AssertPinnedHashes(BalticReplayHarness.Result result, string[] goldenLines)
    {
        var expectedWorld = ulong.Parse(
            goldenLines.First(l => l.StartsWith("WORLD_HASH=", StringComparison.Ordinal))["WORLD_HASH=".Length..]);
        var expectedDetection = ulong.Parse(
            goldenLines.First(l => l.StartsWith("DETECTION_WORLD_HASH=", StringComparison.Ordinal))[
                "DETECTION_WORLD_HASH=".Length..]);
        Assert.That(result.WorldHash, Is.EqualTo(expectedWorld));
        Assert.That(result.DetectionWorldHash, Is.EqualTo(expectedDetection));

        var shaLine = goldenLines.FirstOrDefault(l => l.StartsWith("FINGERPRINT_SHA256=", StringComparison.Ordinal));
        if (shaLine != null)
        {
            var expectedSha = shaLine["FINGERPRINT_SHA256=".Length..].Trim();
            Assert.That(result.FingerprintSha256, Is.EqualTo(expectedSha));
        }
    }

    public static string ResolveRegressionGoldenPath(string fileName) =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "tests", "regression", fileName));
}