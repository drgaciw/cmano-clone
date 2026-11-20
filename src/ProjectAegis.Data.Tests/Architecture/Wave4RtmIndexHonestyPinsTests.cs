namespace ProjectAegis.Data.Tests.Architecture;

using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// Wave 4 adversarial honesty pins for architecture RTM + root master index + GR index.
/// Docs-only corpus gates — no production changes. Pattern: RequirementsHubContractTests / Wave3.
/// </summary>
public sealed class Wave4RtmIndexHonestyPinsTests
{
    [Test]
    public void architecture_rtm_header_has_current_gates_and_w4_stamp()
    {
        var path = ResolveRepoFile("docs", "architecture", "requirements-traceability.md");
        Assert.That(path != null, Is.True, "Could not locate docs/architecture/requirements-traceability.md");
        var text = File.ReadAllText(path!);

        // Gate floor references AGENTS.md §Hard Invariants (or cites current floor ≥1638).
        Assert.That(            text.Contains("AGENTS.md", StringComparison.Ordinal) ||
            text.Contains("≥1638", StringComparison.Ordinal), Is.True,             "RTM must cite current solution test floor / AGENTS.md reference (≥1638)");

        Assert.That(text, Does.Contain("ReplayGolden"));
        Assert.That(text, Does.Contain("6/6"));
        Assert.That(text, Does.Contain("PlayModeSmoke"));
        Assert.That(text, Does.Contain("17144800277401907079"));

        Assert.That(            text.Contains("corpus W4", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("W4 complete", StringComparison.OrdinalIgnoreCase), Is.True,             "RTM header/stamp must mention corpus W4 or W4 complete");
    }

    [Test]
    public void architecture_rtm_has_platform_editor_fr19_section()
    {
        var path = ResolveRepoFile("docs", "architecture", "requirements-traceability.md");
        Assert.That(path != null, Is.True, "Could not locate docs/architecture/requirements-traceability.md");
        var text = File.ReadAllText(path!);

        Assert.That(text, Does.Contain("Platform editor").IgnoreCase);
        Assert.That(text, Does.Contain("FR-19"));
        Assert.That(text, Does.Contain("ADR-011"));

        Assert.That(            text.Contains("IPlatformWorkbookIo", StringComparison.Ordinal) ||
            text.Contains("PlatformWorkbookExporter", StringComparison.Ordinal) ||
            text.Contains("PlatformWorkbookImporter", StringComparison.Ordinal), Is.True,             "RTM platform-editor block must mention IPlatformWorkbookIo / PlatformWorkbookExporter (or similar W4 symbol)");
    }

    [Test]
    public void rtm_closeout_403_is_marked_historical_not_current_alone()
    {
        var path = ResolveRepoFile("docs", "architecture", "requirements-traceability.md");
        Assert.That(path != null, Is.True, "Could not locate docs/architecture/requirements-traceability.md");
        var text = File.ReadAllText(path!);

        // Header / current gates cite AGENTS.md or current floor.
        Assert.That(            text.Contains("AGENTS.md", StringComparison.Ordinal) ||
            text.Contains("≥1638", StringComparison.Ordinal), Is.True,             "RTM must cite current gate floor (≥1638) or AGENTS.md reference");

        // If historical 403/403 remains, it must be labeled historical nearby (not presented as current alone).
        var idx403 = text.IndexOf("403/403", StringComparison.Ordinal);
        if (idx403 < 0)
        {
            // Accept alternate phrasing that still mentions 403 baseline.
            idx403 = text.IndexOf("403", StringComparison.Ordinal);
        }

        if (idx403 >= 0)
        {
            var start = Math.Max(0, idx403 - 200);
            var len = Math.Min(400, text.Length - start);
            var window = text.Substring(start, len);
            Assert.That(                window.Contains("historical", StringComparison.OrdinalIgnoreCase), Is.True,                 "403 closeout must be marked historical (within ~200 chars) so it is not the sole current gate");
        }
    }

    [Test]
    public void root_master_index_uses_current_tracker_and_test_floor()
    {
        var path = ResolveRepoFile("00-Master-Index.md");
        Assert.That(path != null, Is.True, "Could not locate root 00-Master-Index.md");
        var text = File.ReadAllText(path!);

        Assert.That(            text.Contains("implementation-tracker.md", StringComparison.Ordinal) ||
            text.Contains("implementation-tracker-2026-07-04.md", StringComparison.Ordinal), Is.True,             "Root master index must reference implementation-tracker.md or historical 2026-07-04 tracker");
        Assert.That(            text.Contains("AGENTS.md", StringComparison.Ordinal) ||
            text.Contains("≥1638", StringComparison.Ordinal), Is.True,             "Root master index verify/floor must cite AGENTS.md reference or current test floor (≥1638)");

        // Stale sole baseline "(345 tests" must not be the only verify story — 1232 already required above.
        // Soft pin: if 345 appears as the parenthetical sole baseline form, fail.
        Assert.That(text, Does.Not.Contain("(345 tests"));
    }

    [Test]
    public void gr_index_program_note_corpus_complete_editor_active()
    {
        var path = ResolveRepoFile("Game-Requirements", "Game-Requirements-Index.md");
        Assert.That(path != null, Is.True, "Could not locate Game-Requirements/Game-Requirements-Index.md");
        var text = File.ReadAllText(path!);

        Assert.That(            text.Contains("W0–W4", StringComparison.Ordinal) ||
            text.Contains("W0-W4", StringComparison.Ordinal) ||
            Regex.IsMatch(text, @"W0\s*[–-]\s*W4", RegexOptions.IgnoreCase), Is.True,             "GR index must state W0–W4 (or W0-W4) complete");

        Assert.That(            text.Contains("complete", StringComparison.OrdinalIgnoreCase), Is.True,             "GR index program note must mark corpus waves complete");

        Assert.That(            text.Contains("complete on trunk", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Scenario editor (req 11), ME Phase 2, and PE complete", StringComparison.OrdinalIgnoreCase) ||
            (text.Contains("scenario editor", StringComparison.OrdinalIgnoreCase) && text.Contains("complete", StringComparison.OrdinalIgnoreCase)), Is.True,             "GR index must note scenario editor / ME / PE complete on trunk language");
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
