namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

/// <summary>
/// S84-03 / ADR-016: Event graph complexity caps unit tests.
/// Hard cap 32 conditions/event = Error (blocks).
/// Soft warnings for complexity >400 or peak_density >20; export Allowed remains true.
/// Additive coverage for AC-7/11/16 tracks. Cites: qa-plan-scenario-editor-2026-07-01.md #16, sprint-84-event-debugger.md, adr-016-*.md, roadmap-execute-plan-07042026.md, scenario-editor-scope-boundary-2026-07-04.md, design/gdd/agentic-mission-editor.md §4.3.
/// </summary>
public sealed class EventGraphComplexityTests
{
    private static readonly ICatalogReader Catalog = InMemoryCatalogReader.BalticPatrolFixture();
    private static readonly ValidationConfig DefaultConfig = new();

    [Test]
    public void Event_with_33_conditions_emits_cap_exceeded_error()
    {
        var ev = new ScenarioEventDto { Id = "evt-over", TriggerType = "Time", Conditions = Enumerable.Range(0, 33).Select(_ => new ScenarioEventConditionDto { Type = "Time", Result = true }).ToList(), Actions = [] };
        var doc = new ScenarioDocumentDto { Metadata = new ScenarioMetadataDto { TlBranch = "TL-0", DbRef = "baltic_patrol", EditVersion = 1, Seed = 42 }, Events = [ev] };

        var report = new ScenarioValidationEngine().Validate(doc, Catalog, DefaultConfig);
        Assert.That(report.Findings.Any(f => f.Code == "EVENT_CONDITION_CAP_EXCEEDED" && f.Severity == ValidationSeverity.Error), Is.True);
    }

    [Test]
    public void Event_with_exactly_32_conditions_is_under_hard_cap()
    {
        var ev = new ScenarioEventDto { Id = "evt-boundary", TriggerType = "Time", Conditions = Enumerable.Range(0, 32).Select(_ => new ScenarioEventConditionDto { Type = "Time", Result = true }).ToList(), Actions = [] };
        var doc = new ScenarioDocumentDto { Metadata = new ScenarioMetadataDto { TlBranch = "TL-0", DbRef = "baltic_patrol", EditVersion = 1, Seed = 42 }, Events = [ev] };

        var report = new ScenarioValidationEngine().Validate(doc, Catalog, DefaultConfig);
        Assert.That(report.Findings.Any(f => f.Code == "EVENT_CONDITION_CAP_EXCEEDED"), Is.False);
    }

    [Test]
    public void High_complexity_emits_soft_warning_but_export_allowed()
    {
        // Force high E + conds by many events with refs
        var events = Enumerable.Range(0, 50).Select(i => new ScenarioEventDto
        {
            Id = $"e{i}",
            TriggerType = "Time",
            Conditions = [new ScenarioEventConditionDto { Type = "UnitEntersZone", UnitId = "u1", ZoneId = "z" }],
            Actions = []  // no missions to keep clean for gate (other rules)
        }).ToList();
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = "TL-0", DbRef = "baltic_patrol", EditVersion = 1, Seed = 42 },
            Events = events
        };
        var config = new ValidationConfig(ComplexityWarnThreshold: 10); // low to trigger

        var report = new ScenarioValidationEngine().Validate(doc, Catalog, config);
        Assert.That(report.Findings.Any(f => f.Code == "EVENT_GRAPH_COMPLEXITY_HIGH" && f.Severity == ValidationSeverity.Warning), Is.True);

        var (allowed, _) = ScenarioValidationExportGate.EvaluateExport(doc, Catalog, config);
        Assert.That(allowed, Is.True, "Soft warning must not block export per ADR-016");
    }

    [Test]
    public void High_peak_tick_density_emits_soft_warning_but_export_allowed()
    {
        var events = Enumerable.Range(0, 25).Select(i => new ScenarioEventDto
        {
            Id = $"t{i}",
            TriggerType = "Time",
            Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
            Actions = []
        }).ToList();
        var doc = new ScenarioDocumentDto { Metadata = new ScenarioMetadataDto { TlBranch = "TL-0", DbRef = "baltic_patrol", EditVersion = 1, Seed = 42 }, Events = events };
        var config = new ValidationConfig(DensityWarnThreshold: 5);

        var report = new ScenarioValidationEngine().Validate(doc, Catalog, config);
        Assert.That(report.Findings.Any(f => f.Code == "EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH" && f.Severity == ValidationSeverity.Warning), Is.True);

        var (allowed, _) = ScenarioValidationExportGate.EvaluateExport(doc, Catalog, config);
        Assert.That(allowed, Is.True);
    }

    [Test]
    public void Zero_events_produces_no_complexity_findings()
    {
        var doc = new ScenarioDocumentDto { Metadata = new ScenarioMetadataDto { TlBranch = "TL-0", DbRef = "baltic_patrol", EditVersion = 1, Seed = 42 }, Events = [] };
        var report = new ScenarioValidationEngine().Validate(doc, Catalog, DefaultConfig);
        Assert.That(report.Findings.Any(f => f.Code.Contains("EVENT_GRAPH") || f.Code.Contains("CONDITION_CAP")), Is.False);
    }

    [Test]
    public void Export_gate_allows_on_soft_breach_determinism()
    {
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = "TL-0", DbRef = "baltic_patrol", EditVersion = 1, Seed = 42 },
            Events = [new ScenarioEventDto { Id = "e1", TriggerType = "Time", Conditions = [new() { Type = "Time", Result = true }] }]
        };
        var configHigh = new ValidationConfig(ComplexityWarnThreshold: 0, DensityWarnThreshold: 0);

        var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(doc, Catalog, configHigh);
        Assert.That(allowed, Is.True);
        Assert.That(report.Findings.Any(f => f.Severity == ValidationSeverity.Warning), Is.True);
    }

    // Additional coverage additive for 20/20 editor filter pass
    [Test]
    public void Boundary_32_conditions_no_error_and_no_block()
    {
        var conds = Enumerable.Range(0, 32).Select(_ => new ScenarioEventConditionDto { Type = "Time", Result = true }).ToList();
        var doc = new ScenarioDocumentDto { Metadata = new ScenarioMetadataDto { TlBranch = "TL-0", DbRef = "baltic_patrol", EditVersion = 1, Seed = 42 }, Events = [new ScenarioEventDto { Id = "b32", TriggerType = "Time", Conditions = conds }] };
        var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(doc, Catalog, DefaultConfig);
        Assert.That(allowed, Is.True);
        Assert.That(report.Findings.Any(f => f.Code == "EVENT_CONDITION_CAP_EXCEEDED"), Is.False);
    }
}
