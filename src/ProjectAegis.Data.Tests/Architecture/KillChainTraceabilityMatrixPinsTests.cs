using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Architecture;

[TestFixture]
public sealed class KillChainTraceabilityMatrixPinsTests
{
    private static readonly string[] RequiredF2T2EaStages =
    [
        "Find",
        "Fix",
        "Track",
        "Target",
        "Engage",
        "Assess",
    ];

    private static readonly string[] RequiredCrossCuttingLanes =
    [
        "OODA",
        "Authority",
        "Sensor-to-Shooter",
        "Comms",
        "Mission-Command",
        "BDA",
    ];

    private static readonly string[] RequiredHoldOwnerGoIds =
    [
        "DRG-268",
        "DRG-269",
        "DRG-236",
        "DRG-231",
        "DRG-347",
    ];

    [Test]
    public void Kill_chain_traceability_matrix_exists_and_is_linked_from_rtm()
    {
        var repoRoot = FindRepoRoot();
        var matrixPath = Path.Combine(repoRoot, "docs", "architecture", "kill-chain-traceability-matrix.md");
        var rtmPath = Path.Combine(repoRoot, "docs", "architecture", "requirements-traceability.md");

        Assert.That(File.Exists(matrixPath), Is.True, "docs/architecture/kill-chain-traceability-matrix.md must exist");
        Assert.That(File.Exists(rtmPath), Is.True);

        var rtmText = File.ReadAllText(rtmPath);
        Assert.That(rtmText, Does.Contain("kill-chain-traceability-matrix.md"));
        Assert.That(rtmText, Does.Contain("DRG-187"));
    }

    [Test]
    public void Matrix_covers_all_f2t2ea_stages_cross_cutting_lanes_and_valid_repo_paths()
    {
        var repoRoot = FindRepoRoot();
        var matrixPath = Path.Combine(repoRoot, "docs", "architecture", "kill-chain-traceability-matrix.md");
        var text = File.ReadAllText(matrixPath);

        foreach (var stage in RequiredF2T2EaStages)
        {
            Assert.That(text, Does.Contain(stage), $"Matrix missing F2T2EA stage '{stage}'");
        }

        foreach (var lane in RequiredCrossCuttingLanes)
        {
            Assert.That(text, Does.Contain(lane), $"Matrix missing cross-cutting lane '{lane}'");
        }

        var pathMatches = Regex.Matches(text, @"`((?:src|unity|data/scenarios|docs|production)/[^`\s]+\.(?:cs|json|md|uxml|uss|unity))`");
        Assert.That(pathMatches.Count, Is.GreaterThanOrEqualTo(10), "Matrix must cite concrete existing repo paths");
        foreach (Match match in pathMatches)
        {
            var rel = match.Groups[1].Value;
            var full = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(full), Is.True, $"Cited repo path does not exist: {rel}");
        }
    }

    [Test]
    public void Matrix_enforces_honest_evidence_classes_unverified_gaps_drg208_boundary_and_hold_owner_go_list()
    {
        var repoRoot = FindRepoRoot();
        var matrixPath = Path.Combine(repoRoot, "docs", "architecture", "kill-chain-traceability-matrix.md");
        var text = File.ReadAllText(matrixPath);

        Assert.That(text, Does.Contain("Unverified Transitions"));
        Assert.That(text, Does.Contain("headless-tested"));
        Assert.That(text, Does.Contain("proxy-tested"));
        Assert.That(text, Does.Contain("fixture-only"));
        Assert.That(text, Does.Contain("Editor-PlayMode"));
        Assert.That(text, Does.Contain("owner-only"));

        Assert.That(text, Does.Contain("DRG-208"));
        Assert.That(text, Does.Contain("9cd872c7"));
        Assert.That(text, Does.Contain("DRG-193"));
        Assert.That(text, Does.Contain("DRG-201"));
        Assert.That(text, Does.Contain("DRG-244"));

        foreach (var holdId in RequiredHoldOwnerGoIds)
        {
            Assert.That(text, Does.Contain(holdId), $"Matrix must explicitly list hold:owner-go issue {holdId}");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("ProjectAegis.sln not found");
    }
}
