namespace ProjectAegis.Data.Tests.Architecture;

using System.Text.RegularExpressions;
using ProjectAegis.Data.Excel;
using Xunit;

/// <summary>
/// Wave 3 charter honesty pins: FR reverse-refs, Implementation Mapping rewrites,
/// tracker 10b Phase N (not S54 overclaim), and ClosedXML workbook I/O type existence.
/// Docs-only + reflection — no production changes. Pattern: RequirementsHubContractTests.
/// </summary>
public sealed class Wave3RequirementsHonestyContractTests
{
    [Fact]
    public void doc_09_and_10_reverse_ref_FR_08_and_have_implementation_mapping()
    {
        foreach (var name in new[]
                 {
                     "09-Near-Future-Technologies.md",
                     "10-Speculative-Systems.md",
                 })
        {
            var path = ResolveRepoFile("Game-Requirements", "requirements", name);
            Assert.True(path != null, $"Could not locate {name}");
            var text = File.ReadAllText(path!);

            Assert.Contains("FR-08", text, StringComparison.Ordinal);
            Assert.Contains("Implementation Mapping", text, StringComparison.Ordinal);
            Assert.Contains("re-honesty: Wave 3", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void doc_21_reverse_ref_FR_19_and_mapping_not_all_new()
    {
        var path = ResolveRepoFile("Game-Requirements", "requirements", "21-Platform-Editor.md");
        Assert.True(path != null, "Could not locate 21-Platform-Editor.md");
        var text = File.ReadAllText(path!);

        Assert.Contains("FR-19", text, StringComparison.Ordinal);
        Assert.Contains("Implementation Mapping", text, StringComparison.Ordinal);
        // PE-W0–W4 closeout: Design Status is Revised/implementation-aligned (not Draft-only).
        // Accept either legacy Wave 3 re-honesty pin or PE completion honesty language.
        Assert.True(
            text.Contains("re-honesty: Wave 3", StringComparison.Ordinal) ||
            text.Contains("Revised — implementation-aligned", StringComparison.Ordinal) ||
            text.Contains("PE-W0–W4 COMPLETE", StringComparison.Ordinal),
            "Doc 21 must carry Wave 3 or PE completion honesty language");
        Assert.DoesNotContain("Design Status remains **Draft**", text, StringComparison.Ordinal);

        var mapIdx = text.IndexOf("## Implementation Mapping", StringComparison.Ordinal);
        Assert.True(mapIdx >= 0, "Implementation Mapping heading missing");
        var mappingSection = text[mapIdx..];

        // Honest rewrite: mapping must claim Shipped somewhere — not an all-New status table.
        Assert.Contains("Shipped", mappingSection, StringComparison.Ordinal);
        Assert.False(
            Regex.IsMatch(
                mappingSection,
                @"\|[^|\n]*\|\s*\*\*New\*\*\s*\|",
                RegexOptions.Multiline) &&
            !mappingSection.Contains("Shipped", StringComparison.Ordinal),
            "Implementation Mapping must not be status-all-New without Shipped claims");
    }

    [Fact]
    public void tracker_10b_is_phase_n_not_on_main_not_implemented_s54()
    {
        var path = ResolveRepoFile("Game-Requirements", "implementation-tracker-2026-07-04.md");
        Assert.True(path != null, "Could not locate implementation-tracker-2026-07-04.md");
        var text = File.ReadAllText(path!);

        Assert.Contains("10b", text, StringComparison.Ordinal);
        Assert.Contains("Phase N / not on main", text, StringComparison.Ordinal);

        var tenBLine = text
            .Split('\n')
            .FirstOrDefault(l => l.Contains("| 10b |", StringComparison.Ordinal));
        Assert.True(tenBLine != null, "Tracker row for | 10b | not found");
        Assert.Contains("Phase N", tenBLine!, StringComparison.Ordinal);
        Assert.DoesNotContain("Implemented (S54)", tenBLine!, StringComparison.Ordinal);
    }

    [Fact]
    public void platform_workbook_io_closedxml_type_exists()
    {
        // Production adapter lives in ProjectAegis.Data.Excel (referenced by Data.Tests).
        var type = typeof(ClosedXmlPlatformWorkbookIo);
        Assert.Equal("ClosedXmlPlatformWorkbookIo", type.Name);
        Assert.Equal("ProjectAegis.Data.Excel", type.Namespace);

        // Reflection pin: type resolvable from Excel assembly by name (rg-stable contract).
        var excelAsm = typeof(ClosedXmlPlatformWorkbookIo).Assembly;
        var byName = excelAsm.GetType("ProjectAegis.Data.Excel.ClosedXmlPlatformWorkbookIo", throwOnError: false);
        Assert.NotNull(byName);
        Assert.True(byName!.IsClass, "ClosedXmlPlatformWorkbookIo must be a class type");
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
