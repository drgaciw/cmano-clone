namespace ProjectAegis.Data.Tests.Architecture;

using Xunit;

/// <summary>
/// Wave 4 corpus gate artifact pins: consistency verdict, design-review memo,
/// epic/story-005 Complete, and tracker W0–W4 program note with 10b still Phase N.
/// Docs-only — no production changes. Pattern: Wave3RequirementsHonestyContractTests.
/// </summary>
public sealed class Wave4CorpusGateArtifactsPinsTests
{
    [Fact]
    public void consistency_report_2026_07_08_verdict_is_zero_blocker()
    {
        var path = ResolveRepoFile("docs", "reports", "requirements-consistency-2026-07-08.md");
        Assert.True(path != null, "Could not locate docs/reports/requirements-consistency-2026-07-08.md");
        var text = File.ReadAllText(path!);

        // Accept plain or bold markdown form of the gate verdict.
        Assert.True(
            text.Contains("0 BLOCKER", StringComparison.Ordinal) ||
            text.Contains("**0 BLOCKER**", StringComparison.Ordinal),
            "Consistency report must contain '0 BLOCKER' (or **0 BLOCKER**) verdict");
    }

    [Fact]
    public void design_review_wave4_memo_exists_and_covers_sample_docs()
    {
        var path = ResolveRepoFile(
            "production",
            "qa",
            "requirements-corpus-w4-design-review-2026-07-08.md");
        Assert.True(
            path != null,
            "Could not locate production/qa/requirements-corpus-w4-design-review-2026-07-08.md");
        var text = File.ReadAllText(path!);

        // Sample docs 01, 04, 14, 21 (by number or title) and APPROVED verdict.
        Assert.True(
            text.Contains("01", StringComparison.Ordinal) ||
            text.Contains("Project Overview", StringComparison.Ordinal) ||
            text.Contains("Hub", StringComparison.Ordinal),
            "Design review must mention doc 01 / hub / Project Overview");
        Assert.True(
            text.Contains("04", StringComparison.Ordinal) ||
            text.Contains("Delegation", StringComparison.Ordinal),
            "Design review must mention doc 04 / Delegation");
        Assert.True(
            text.Contains("14", StringComparison.Ordinal) ||
            text.Contains("Engagement", StringComparison.Ordinal),
            "Design review must mention doc 14 / Engagement");
        Assert.True(
            text.Contains("21", StringComparison.Ordinal) ||
            text.Contains("Platform Editor", StringComparison.Ordinal),
            "Design review must mention doc 21 / Platform Editor");
        Assert.Contains("APPROVED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void epic_story_005_complete_and_waves_closed()
    {
        var epicPath = ResolveRepoFile(
            "production",
            "epics",
            "requirements-corpus-maturity",
            "EPIC.md");
        Assert.True(
            epicPath != null,
            "Could not locate production/epics/requirements-corpus-maturity/EPIC.md");
        var epic = File.ReadAllText(epicPath!);

        Assert.Contains("Complete", epic, StringComparison.Ordinal);
        // Story 005 row must be Complete (wave 4 corpus gate).
        var story005Line = epic
            .Split('\n')
            .FirstOrDefault(l =>
                l.Contains("005", StringComparison.Ordinal) &&
                l.Contains("story-005", StringComparison.Ordinal));
        Assert.True(story005Line != null, "EPIC.md must list story 005 / story-005");
        Assert.Contains("Complete", story005Line!, StringComparison.Ordinal);

        var storyPath = ResolveRepoFile(
            "production",
            "epics",
            "requirements-corpus-maturity",
            "story-005-corpus-gate.md");
        Assert.True(
            storyPath != null,
            "Could not locate production/epics/requirements-corpus-maturity/story-005-corpus-gate.md");
        var story = File.ReadAllText(storyPath!);
        Assert.Contains("Status", story, StringComparison.Ordinal);
        Assert.Contains("Complete", story, StringComparison.Ordinal);
    }

    [Fact]
    public void tracker_program_note_w0_w4_complete()
    {
        var path = ResolveRepoFile("Game-Requirements", "implementation-tracker-2026-07-04.md");
        Assert.True(path != null, "Could not locate implementation-tracker-2026-07-04.md");
        var text = File.ReadAllText(path!);

        // Program note: W0–W4 (en-dash or hyphen) complete language.
        Assert.True(
            text.Contains("W0–W4", StringComparison.Ordinal) ||
            text.Contains("W0-W4", StringComparison.Ordinal),
            "Tracker must mention W0–W4 or W0-W4 corpus program");
        Assert.True(
            text.Contains("complete", StringComparison.OrdinalIgnoreCase),
            "Tracker program note must include complete language");

        // Row | 10b | remains Phase N — not Implemented (S54).
        var tenBLine = text
            .Split('\n')
            .FirstOrDefault(l => l.Contains("| 10b |", StringComparison.Ordinal));
        Assert.True(tenBLine != null, "Tracker row for | 10b | not found");
        Assert.Contains("Phase N", tenBLine!, StringComparison.Ordinal);
        Assert.DoesNotContain("Implemented (S54)", tenBLine!, StringComparison.Ordinal);
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
