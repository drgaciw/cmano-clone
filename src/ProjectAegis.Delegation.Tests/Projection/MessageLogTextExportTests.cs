using System.Text;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// DRG-412 PR1: headless message-log text export is stable plain text and byte-identical
/// across identical projected lines. New golden fixture — not ReplayGolden.
/// </summary>
[TestFixture]
public sealed class MessageLogTextExportTests
{
    [Test]
    public void FormatLine_emits_sim_time_category_subject_message_tab_separated()
    {
        var line = new MessageLogLine(7, 12.5, "WEAPON_LAUNCH", "Unit u1 launched engagement 7", "u1");

        var formatted = MessageLogTextExport.FormatLine(line);

        Assert.That(formatted, Is.EqualTo("12.500000\tWEAPON_LAUNCH\tu1\tUnit u1 launched engagement 7"));
    }

    [Test]
    public void FormatLine_empty_subject_when_unit_id_null()
    {
        var line = new MessageLogLine(1, 0, "MODE", "Mode Agent → Human");

        var formatted = MessageLogTextExport.FormatLine(line);

        Assert.That(formatted, Is.EqualTo("0.000000\tMODE\t\tMode Agent → Human"));
    }

    [Test]
    public void Render_joins_lines_with_LF_and_trailing_newline()
    {
        var lines = new[]
        {
            new MessageLogLine(1, 1, "WEAPON_LAUNCH", "Unit u1 launched engagement 7", "u1"),
            new MessageLogLine(2, 2, "HIT", "Weapon hit hostile-1 (engagement 7)", "u1"),
        };

        var text = MessageLogTextExport.Render(lines);

        Assert.That(text, Is.EqualTo(
            "1.000000\tWEAPON_LAUNCH\tu1\tUnit u1 launched engagement 7\n" +
            "2.000000\tHIT\tu1\tWeapon hit hostile-1 (engagement 7)\n"));
    }

    [Test]
    public void Render_null_lines_throws()
    {
        Assert.Throws<ArgumentNullException>(() => MessageLogTextExport.Render(null!));
    }

    [Test]
    public void Two_identical_projections_are_byte_identical_and_match_golden()
    {
        var log = SampleCombatLog();
        var first = MessageLogTextExport.Render(MessageLogProjection.Project(log));
        var second = MessageLogTextExport.Render(MessageLogProjection.Project(log));
        var golden = File.ReadAllText(GoldenPath(), Encoding.UTF8);

        Assert.That(Encoding.UTF8.GetBytes(first), Is.EqualTo(Encoding.UTF8.GetBytes(second)));
        Assert.That(first, Is.EqualTo(golden));
        Assert.That(second, Is.EqualTo(golden));
    }

    [Test]
    public void WriteToPath_writes_utf8_bytes_without_mutating_decision_log()
    {
        var log = SampleCombatLog();
        var before = log.ChronologicalEntries().Count;
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "drg-412-export.txt");

        MessageLogTextExport.WriteToPath(MessageLogProjection.Project(log), path);

        Assert.That(log.ChronologicalEntries(), Has.Count.EqualTo(before));
        Assert.That(File.ReadAllText(path, Encoding.UTF8), Is.EqualTo(File.ReadAllText(GoldenPath(), Encoding.UTF8)));
    }

    [Test]
    public void Binder_categorized_view_keeps_bracket_prefix()
    {
        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "KILL_CONFIRMED", "Hostile destroyed", "u1"),
        };

        var panel = MessageLogPanelBinder.Bind(lines, MessageLogBindingSource.FullDecisionLog, MessageLogPanelViewMode.Categorized);

        Assert.That(panel.ViewMode, Is.EqualTo(MessageLogPanelViewMode.Categorized));
        Assert.That(panel.Rows[0].DisplayLine, Is.EqualTo("[KILL_CONFIRMED] Hostile destroyed"));
    }

    [Test]
    public void Binder_raw_view_uses_export_format_without_mutating_lines()
    {
        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "KILL_CONFIRMED", "Hostile destroyed", "u1"),
        };

        var panel = MessageLogPanelBinder.Bind(lines, MessageLogBindingSource.FullDecisionLog, MessageLogPanelViewMode.Raw);

        Assert.That(panel.ViewMode, Is.EqualTo(MessageLogPanelViewMode.Raw));
        Assert.That(panel.Rows[0].DisplayLine, Is.EqualTo(MessageLogTextExport.FormatLine(lines[0])));
        Assert.That(lines[0].Text, Is.EqualTo("Hostile destroyed"));
    }

    [Test]
    public void Binder_export_path_matches_text_export_render()
    {
        var lines = MessageLogProjection.Project(SampleCombatLog());
        var panel = MessageLogPanelBinder.Bind(lines, MessageLogBindingSource.FullDecisionLog, MessageLogPanelViewMode.Raw);

        var exported = MessageLogPanelBinder.ExportText(panel);

        Assert.That(exported, Is.EqualTo(MessageLogTextExport.Render(lines)));
    }

    [Test]
    public void Source_fence_export_has_no_hard_no_refs()
    {
        var source = File.ReadAllText(ExportSourcePath());

        Assert.That(source, Does.Not.Contain("UnityEngine"));
        Assert.That(source, Does.Not.Contain("MonoBehaviour"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
        Assert.That(source, Does.Not.Contain("SimulationSession"));
        Assert.That(source, Does.Not.Contain("DelegationBridge"));
        Assert.That(source, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(source, Does.Not.Contain("DateTime."));
        Assert.That(source, Does.Contain("CultureInfo.InvariantCulture"));
    }

    private static DecisionLog SampleCombatLog()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 1, 1, new TargetId("u1"), 7, true, null, new TargetId("hostile-1"), "Gun"));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0, 2, 2, new TargetId("u1"), new TargetId("hostile-1"), 7,
            EngagementOutcomeCodes.Hit, 0.9));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 3, 3, new AgentId("a1"), new TargetId("u1"), 1,
            FireAbortReason.WeaponsTight, OrderKind.Engage));
        return log;
    }

    private static string GoldenPath()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "ProjectAegis.Delegation.Tests",
                "Projection",
                "Fixtures",
                "message-log-text-export.golden.txt");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("message-log-text-export.golden.txt");
    }

    private static string ExportSourcePath()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "ProjectAegis.Delegation",
                "Projection",
                "MessageLogTextExport.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("MessageLogTextExport.cs");
    }
}
