namespace ProjectAegis.Data.Tests.Architecture;

using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// Wave 4 adversarial honesty pins for architecture RTM + root master index + GR index.
/// Docs-only corpus gates — no production changes. Pattern: RequirementsHubContractTests / Wave3.
/// </summary>
public sealed class Wave4RtmIndexHonestyPinsTests
{
    [Fact]
    public void architecture_rtm_header_has_current_gates_and_w4_stamp()
    {
        var path = ResolveRepoFile("docs", "architecture", "requirements-traceability.md");
        Assert.True(path != null, "Could not locate docs/architecture/requirements-traceability.md");
        var text = File.ReadAllText(path!);

        // Current gate floor (≥1232 or plain 1232).
        Assert.True(
            text.Contains("≥1232", StringComparison.Ordinal) ||
            text.Contains("1232", StringComparison.Ordinal),
            "RTM must cite current solution test floor 1232 / ≥1232");

        Assert.Contains("ReplayGolden", text, StringComparison.Ordinal);
        Assert.Contains("6/6", text, StringComparison.Ordinal);
        Assert.Contains("PlayModeSmoke", text, StringComparison.Ordinal);
        Assert.Contains("18/18", text, StringComparison.Ordinal);
        Assert.Contains("17144800277401907079", text, StringComparison.Ordinal);

        Assert.True(
            text.Contains("corpus W4", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("W4 complete", StringComparison.OrdinalIgnoreCase),
            "RTM header/stamp must mention corpus W4 or W4 complete");
    }

    [Fact]
    public void architecture_rtm_has_platform_editor_fr19_section()
    {
        var path = ResolveRepoFile("docs", "architecture", "requirements-traceability.md");
        Assert.True(path != null, "Could not locate docs/architecture/requirements-traceability.md");
        var text = File.ReadAllText(path!);

        Assert.Contains("Platform editor", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FR-19", text, StringComparison.Ordinal);
        Assert.Contains("ADR-011", text, StringComparison.Ordinal);

        Assert.True(
            text.Contains("IPlatformWorkbookIo", StringComparison.Ordinal) ||
            text.Contains("PlatformWorkbookExporter", StringComparison.Ordinal) ||
            text.Contains("PlatformWorkbookImporter", StringComparison.Ordinal),
            "RTM platform-editor block must mention IPlatformWorkbookIo / PlatformWorkbookExporter (or similar W4 symbol)");
    }

    [Fact]
    public void rtm_closeout_403_is_marked_historical_not_current_alone()
    {
        var path = ResolveRepoFile("docs", "architecture", "requirements-traceability.md");
        Assert.True(path != null, "Could not locate docs/architecture/requirements-traceability.md");
        var text = File.ReadAllText(path!);

        // Header / current gates still pin 1232.
        Assert.True(
            text.Contains("≥1232", StringComparison.Ordinal) ||
            text.Contains("1232", StringComparison.Ordinal),
            "RTM must still cite current gate floor 1232");

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
            Assert.True(
                window.Contains("historical", StringComparison.OrdinalIgnoreCase),
                "403 closeout must be marked historical (within ~200 chars) so it is not the sole current gate");
        }
    }

    [Fact]
    public void root_master_index_uses_current_tracker_and_test_floor()
    {
        var path = ResolveRepoFile("00-Master-Index.md");
        Assert.True(path != null, "Could not locate root 00-Master-Index.md");
        var text = File.ReadAllText(path!);

        Assert.Contains("implementation-tracker-2026-07-04.md", text, StringComparison.Ordinal);
        Assert.True(
            text.Contains("≥1232", StringComparison.Ordinal) ||
            text.Contains("1232", StringComparison.Ordinal),
            "Root master index verify/floor must cite ≥1232 or 1232");

        // Stale sole baseline "(345 tests" must not be the only verify story — 1232 already required above.
        // Soft pin: if 345 appears as the parenthetical sole baseline form, fail.
        Assert.DoesNotContain("(345 tests", text, StringComparison.Ordinal);
    }

    [Fact]
    public void gr_index_program_note_corpus_complete_editor_active()
    {
        var path = ResolveRepoFile("Game-Requirements", "Game-Requirements-Index.md");
        Assert.True(path != null, "Could not locate Game-Requirements/Game-Requirements-Index.md");
        var text = File.ReadAllText(path!);

        Assert.True(
            text.Contains("W0–W4", StringComparison.Ordinal) ||
            text.Contains("W0-W4", StringComparison.Ordinal) ||
            Regex.IsMatch(text, @"W0\s*[–-]\s*W4", RegexOptions.IgnoreCase),
            "GR index must state W0–W4 (or W0-W4) complete");

        Assert.True(
            text.Contains("complete", StringComparison.OrdinalIgnoreCase),
            "GR index program note must mark corpus waves complete");

        Assert.True(
            text.Contains("S81", StringComparison.Ordinal) ||
            text.Contains("scenario editor", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("editor", StringComparison.OrdinalIgnoreCase) &&
            text.Contains("active", StringComparison.OrdinalIgnoreCase),
            "GR index must note scenario editor / S81 active language");
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
