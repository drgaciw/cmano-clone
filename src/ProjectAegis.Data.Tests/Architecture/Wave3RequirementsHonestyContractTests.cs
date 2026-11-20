namespace ProjectAegis.Data.Tests.Architecture;

using System.Text.RegularExpressions;
using Excel;
using NUnit.Framework;

/// <summary>
/// Wave 3 charter honesty pins: FR reverse-refs, Implementation Mapping rewrites,
/// tracker 10b Phase N (not S54 overclaim), and ClosedXML workbook I/O type existence.
/// Docs-only + reflection — no production changes. Pattern: RequirementsHubContractTests.
/// </summary>
public sealed class Wave3RequirementsHonestyContractTests
{
    [Test]
    public void doc_09_and_10_reverse_ref_FR_08_and_have_implementation_mapping()
    {
        foreach (var name in new[]
                 {
                     "09-Near-Future-Technologies.md",
                     "10-Speculative-Systems.md",
                 })
        {
            var path = ResolveRepoFile("Game-Requirements", "requirements", name);
            Assert.That(path != null, Is.True, $"Could not locate {name}");
            var text = File.ReadAllText(path!);

            Assert.That(text, Does.Contain("FR-08"));
            Assert.That(text, Does.Contain("Implementation Mapping"));
            Assert.That(text, Does.Contain("re-honesty: Wave 3"));
        }
    }

    [Test]
    public void doc_21_reverse_ref_FR_19_and_mapping_not_all_new()
    {
        var path = ResolveRepoFile("Game-Requirements", "requirements", "21-Platform-Editor.md");
        Assert.That(path != null, Is.True, "Could not locate 21-Platform-Editor.md");
        var text = File.ReadAllText(path!);

        Assert.That(text, Does.Contain("FR-19"));
        Assert.That(text, Does.Contain("Implementation Mapping"));
        // PE-W0–W4 closeout: Design Status is Revised/implementation-aligned (not Draft-only).
        // Accept either legacy Wave 3 re-honesty pin or PE completion honesty language.
        Assert.That(            text.Contains("re-honesty: Wave 3", StringComparison.Ordinal) ||
            text.Contains("Revised — implementation-aligned", StringComparison.Ordinal) ||
            text.Contains("PE-W0–W4 COMPLETE", StringComparison.Ordinal), Is.True,             "Doc 21 must carry Wave 3 or PE completion honesty language");
        Assert.That(text, Does.Not.Contain("Design Status remains **Draft**"));

        var mapIdx = text.IndexOf("## Implementation Mapping", StringComparison.Ordinal);
        Assert.That(mapIdx >= 0, Is.True, "Implementation Mapping heading missing");
        var mappingSection = text[mapIdx..];

        // Honest rewrite: mapping must claim Shipped somewhere — not an all-New status table.
        Assert.That(mappingSection, Does.Contain("Shipped"));
        Assert.That(            Regex.IsMatch(
                mappingSection,
                @"\|[^|\n]*\|\s*\*\*New\*\*\s*\|",
                RegexOptions.Multiline) &&
            !mappingSection.Contains("Shipped", StringComparison.Ordinal), Is.False,             "Implementation Mapping must not be status-all-New without Shipped claims");
    }

    [Test]
    public void tracker_10b_is_phase_n_not_on_main_not_implemented_s54()
    {
        var path = ResolveRepoFile("Game-Requirements", "implementation-tracker-2026-07-04.md");
        Assert.That(path != null, Is.True, "Could not locate implementation-tracker-2026-07-04.md");
        var text = File.ReadAllText(path!);

        Assert.That(text, Does.Contain("10b"));
        Assert.That(text, Does.Contain("Phase N / not on main"));

        var tenBLine = text
            .Split('\n')
            .FirstOrDefault(l => l.Contains("| 10b |", StringComparison.Ordinal));
        Assert.That(tenBLine != null, Is.True, "Tracker row for | 10b | not found");
        Assert.That(tenBLine, Does.Contain("Phase N"));
        Assert.That(tenBLine, Does.Not.Contain("Implemented (S54)"));
    }

    [Test]
    public void platform_workbook_io_closedxml_type_exists()
    {
        // Production adapter lives in ProjectAegis.Data.Excel (referenced by Data.Tests).
        var type = typeof(ClosedXmlPlatformWorkbookIo);
        Assert.That(type.Name, Is.EqualTo("ClosedXmlPlatformWorkbookIo"));
        Assert.That(type.Namespace, Is.EqualTo("ProjectAegis.Data.Excel"));

        // Reflection pin: type resolvable from Excel assembly by name (rg-stable contract).
        var excelAsm = typeof(ClosedXmlPlatformWorkbookIo).Assembly;
        var byName = excelAsm.GetType("ProjectAegis.Data.Excel.ClosedXmlPlatformWorkbookIo", throwOnError: false);
        Assert.That(byName, Is.Not.Null);
        Assert.That(byName!.IsClass, Is.True, "ClosedXmlPlatformWorkbookIo must be a class type");
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
