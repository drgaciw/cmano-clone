namespace ProjectAegis.Data.Tests.Architecture;

using NUnit.Framework;

/// <summary>
/// Wave 4 corpus gate artifact pins: consistency verdict, design-review memo,
/// epic/story-005 Complete, and tracker W0–W4 program note with 10b still Phase N.
/// Docs-only — no production changes. Pattern: Wave3RequirementsHonestyContractTests.
/// </summary>
public sealed class Wave4CorpusGateArtifactsPinsTests
{
    [Test]
    public void consistency_report_2026_07_08_verdict_is_zero_blocker()
    {
        var path = ResolveRepoFile("docs", "reports", "requirements-consistency-2026-07-08.md");
        Assert.That(path != null, Is.True, "Could not locate docs/reports/requirements-consistency-2026-07-08.md");
        var text = File.ReadAllText(path!);

        // Accept plain or bold markdown form of the gate verdict.
        Assert.That(            text.Contains("0 BLOCKER", StringComparison.Ordinal) ||
            text.Contains("**0 BLOCKER**", StringComparison.Ordinal), Is.True,             "Consistency report must contain '0 BLOCKER' (or **0 BLOCKER**) verdict");
    }

    [Test]
    public void design_review_wave4_memo_exists_and_covers_sample_docs()
    {
        var path = ResolveRepoFile(
            "production",
            "qa",
            "requirements-corpus-w4-design-review-2026-07-08.md");
        Assert.That(            path != null, Is.True,             "Could not locate production/qa/requirements-corpus-w4-design-review-2026-07-08.md");
        var text = File.ReadAllText(path!);

        // Sample docs 01, 04, 14, 21 (by number or title) and APPROVED verdict.
        Assert.That(            text.Contains("01", StringComparison.Ordinal) ||
            text.Contains("Project Overview", StringComparison.Ordinal) ||
            text.Contains("Hub", StringComparison.Ordinal), Is.True,             "Design review must mention doc 01 / hub / Project Overview");
        Assert.That(            text.Contains("04", StringComparison.Ordinal) ||
            text.Contains("Delegation", StringComparison.Ordinal), Is.True,             "Design review must mention doc 04 / Delegation");
        Assert.That(            text.Contains("14", StringComparison.Ordinal) ||
            text.Contains("Engagement", StringComparison.Ordinal), Is.True,             "Design review must mention doc 14 / Engagement");
        Assert.That(            text.Contains("21", StringComparison.Ordinal) ||
            text.Contains("Platform Editor", StringComparison.Ordinal), Is.True,             "Design review must mention doc 21 / Platform Editor");
        Assert.That(text, Does.Contain("APPROVED"));
    }

    [Test]
    public void epic_story_005_complete_and_waves_closed()
    {
        var epicPath = ResolveRepoFile(
            "production",
            "epics",
            "requirements-corpus-maturity",
            "EPIC.md");
        Assert.That(            epicPath != null, Is.True,             "Could not locate production/epics/requirements-corpus-maturity/EPIC.md");
        var epic = File.ReadAllText(epicPath!);

        Assert.That(epic, Does.Contain("Complete"));
        // Story 005 row must be Complete (wave 4 corpus gate).
        var story005Line = epic
            .Split('\n')
            .FirstOrDefault(l =>
                l.Contains("005", StringComparison.Ordinal) &&
                l.Contains("story-005", StringComparison.Ordinal));
        Assert.That(story005Line != null, Is.True, "EPIC.md must list story 005 / story-005");
        Assert.That(story005Line, Does.Contain("Complete"));

        var storyPath = ResolveRepoFile(
            "production",
            "epics",
            "requirements-corpus-maturity",
            "story-005-corpus-gate.md");
        Assert.That(            storyPath != null, Is.True,             "Could not locate production/epics/requirements-corpus-maturity/story-005-corpus-gate.md");
        var story = File.ReadAllText(storyPath!);
        Assert.That(story, Does.Contain("Status"));
        Assert.That(story, Does.Contain("Complete"));
    }

    [Test]
    public void tracker_program_note_w0_w4_complete()
    {
        var path = ResolveRepoFile("Game-Requirements", "implementation-tracker-2026-07-04.md");
        Assert.That(path != null, Is.True, "Could not locate implementation-tracker-2026-07-04.md");
        var text = File.ReadAllText(path!);

        // Program note: W0–W4 (en-dash or hyphen) complete language.
        Assert.That(            text.Contains("W0–W4", StringComparison.Ordinal) ||
            text.Contains("W0-W4", StringComparison.Ordinal), Is.True,             "Tracker must mention W0–W4 or W0-W4 corpus program");
        Assert.That(            text.Contains("complete", StringComparison.OrdinalIgnoreCase), Is.True,             "Tracker program note must include complete language");

        // Row | 10b | remains Phase N — not Implemented (S54).
        var tenBLine = text
            .Split('\n')
            .FirstOrDefault(l => l.Contains("| 10b |", StringComparison.Ordinal));
        Assert.That(tenBLine != null, Is.True, "Tracker row for | 10b | not found");
        Assert.That(tenBLine, Does.Contain("Phase N"));
        Assert.That(tenBLine, Does.Not.Contain("Implemented (S54)"));
    }

    private static string? ResolveRepoFile(params string[] relativeSegments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(new[] { dir.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
