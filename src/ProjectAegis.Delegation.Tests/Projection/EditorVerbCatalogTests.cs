using System.Reflection;
using System.Text.RegularExpressions;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// S125-05 / AUTH-03 (Doc 11 / Doc 21 verb honesty): the verb table must match <see cref="IWriteGate"/>,
/// the shipped UXML buttons, the CLI switch, the source call sites, and the documented table.
/// </summary>
[TestFixture]
public sealed class EditorVerbCatalogTests
{
    private static readonly Regex GateCall = new(@"\.(Propose\w+Batch|ApproveBatch|RejectBatch|ListPendingBatches)\(", RegexOptions.Compiled);

    /// <summary>Files whose direct gate calls must equal the verb's declared <c>GateMethods</c>.</summary>
    private static readonly Dictionary<string, string[]> GateSources = new(StringComparer.Ordinal)
    {
        ["pe.propose"] = ["src/ProjectAegis.Data/Platform/PlatformWorkbookImporter.cs"],
        ["catalog.write_propose"] = ["src/ProjectAegis.MissionEditor.Cli/CatalogWriteProposeCommand.cs"],
        ["catalog.import_markdown"] = ["src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs"],
        ["pe.approve"] = ["src/ProjectAegis.MissionEditor.Cli/CatalogWriteApproveCommand.cs"],
        ["catalog.osint_approve"] = ["src/ProjectAegis.MissionEditor.Cli/OsintStagingReviewCommand.cs"],
        ["catalog.osint_pending"] = ["src/ProjectAegis.MissionEditor.Cli/OsintStagingReviewCommand.cs"],
        ["me.save"] = ["src/ProjectAegis.Data/Scenario/Authoring/ScenarioAuthoringSession.cs"],
        ["me.export"] = ["src/ProjectAegis.Data/Scenario/Authoring/ScenarioSaveExportGate.cs"],
    };

    private static IEnumerable<EditorVerbEntry> Entries => EditorVerbCatalog.Entries;

    [Test]
    public void Verb_ids_are_unique()
    {
        Assert.That(Entries.Select(e => e.VerbId), Is.Unique);
    }

    [Test]
    public void Every_verb_has_a_description_and_the_surface_its_id_names()
    {
        foreach (var entry in Entries)
        {
            Assert.That(entry.Description, Is.Not.Empty, entry.VerbId);
            var expectedSurface = entry.VerbId.Split('.')[0] switch
            {
                "pe" => EditorSurface.PlatformEditor,
                "me" => EditorSurface.MissionEditor,
                "catalog" => EditorSurface.CatalogTools,
                var prefix => throw new AssertionException($"{entry.VerbId}: unknown verb prefix '{prefix}'"),
            };
            Assert.That(entry.Surface, Is.EqualTo(expectedSurface), entry.VerbId);
        }

        Assert.That(EditorVerbCatalog.Get("pe.propose").Description, Does.Contain("until Approve"));
        Assert.That(EditorVerbCatalog.Get("me.save").Description, Does.Contain("no write gate"));
    }

    [Test]
    public void Every_declared_gate_method_exists_on_IWriteGate_and_matches_operation()
    {
        var gateMethods = typeof(IWriteGate).GetMethods().Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in Entries)
        {
            foreach (var method in entry.GateMethods)
            {
                Assert.That(gateMethods, Does.Contain(method), $"{entry.VerbId}: {method} is not an IWriteGate operation");
                var expected = entry.GateOperation switch
                {
                    EditorWriteGateOperation.Propose => method.StartsWith("Propose", StringComparison.Ordinal),
                    EditorWriteGateOperation.Approve => method == nameof(IWriteGate.ApproveBatch),
                    EditorWriteGateOperation.Reject => method == nameof(IWriteGate.RejectBatch),
                    EditorWriteGateOperation.ListPending => method == nameof(IWriteGate.ListPendingBatches),
                    _ => false,
                };
                Assert.That(expected, Is.True, $"{entry.VerbId}: {method} does not match {entry.GateOperation}");
            }

            Assert.That(
                entry.GateOperation == EditorWriteGateOperation.None,
                Is.EqualTo(entry.GateMethods.Count == 0),
                $"{entry.VerbId}: gate operation and gate methods disagree");
        }
    }

    [Test]
    public void Every_mutating_IWriteGate_operation_has_an_honest_verb()
    {
        var mutating = typeof(IWriteGate).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .Where(n => n.StartsWith("Propose", StringComparison.Ordinal) || n is "ApproveBatch" or "RejectBatch")
            .Distinct()
            .ToArray();
        var covered = Entries.SelectMany(e => e.GateMethods).ToHashSet(StringComparer.Ordinal);

        Assert.That(mutating, Is.SubsetOf(covered), "a CatalogWriteGate operation has no UI/CLI verb mapping");
    }

    [Test]
    public void Labels_say_what_the_gate_does()
    {
        foreach (var entry in Entries)
        {
            var verb = EditorVerbCatalog.VerbWord(entry.UiLabel);
            switch (entry.GateOperation)
            {
                case EditorWriteGateOperation.Propose:
                    Assert.That(verb, Is.EqualTo("Propose"), entry.VerbId);
                    Assert.That(entry.Effect, Is.EqualTo(EditorVerbEffect.StagedBatch), entry.VerbId);
                    break;
                case EditorWriteGateOperation.Approve:
                    Assert.That(verb, Is.EqualTo("Approve"), entry.VerbId);
                    Assert.That(entry.Effect, Is.EqualTo(EditorVerbEffect.CommittedBatch), entry.VerbId);
                    break;
                case EditorWriteGateOperation.Reject:
                    Assert.That(verb, Is.EqualTo("Reject"), entry.VerbId);
                    Assert.That(entry.Effect, Is.EqualTo(EditorVerbEffect.DiscardedBatch), entry.VerbId);
                    break;
                case EditorWriteGateOperation.ListPending:
                    Assert.That(entry.Effect, Is.EqualTo(EditorVerbEffect.ReadOnly), entry.VerbId);
                    break;
                case EditorWriteGateOperation.None:
                    Assert.That(verb, Is.Not.AnyOf("Propose", "Approve", "Reject", "Commit"), entry.VerbId);
                    Assert.That(entry.Effect, Is.Not.AnyOf(EditorVerbEffect.StagedBatch, EditorVerbEffect.CommittedBatch, EditorVerbEffect.DiscardedBatch), entry.VerbId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry.GateOperation), entry.GateOperation, null);
            }

            if (verb == "Save")
            {
                Assert.That(entry.Effect, Is.EqualTo(EditorVerbEffect.DraftFile), entry.VerbId);
            }
        }
    }

    [Test]
    public void Uxml_buttons_exist_and_use_the_same_verb_word()
    {
        var uxml = Directory.GetFiles(Path.Combine(RepoRoot(), "unity", "ProjectAegis", "Assets", "UI"), "*.uxml", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .ToArray();
        foreach (var entry in Entries.Where(e => e.UxmlButtonName is not null))
        {
            var pattern = new Regex($"<ui:Button[^>]*name=\"{Regex.Escape(entry.UxmlButtonName!)}\"[^>]*text=\"([^\"]*)\"");
            var match = uxml.Select(t => pattern.Match(t)).FirstOrDefault(m => m.Success);
            Assert.That(match, Is.Not.Null, $"{entry.VerbId}: UXML button '{entry.UxmlButtonName}' not found");
            Assert.That(
                EditorVerbCatalog.VerbWord(match!.Groups[1].Value),
                Is.EqualTo(EditorVerbCatalog.VerbWord(entry.UiLabel)),
                $"{entry.VerbId}: UXML text '{match.Groups[1].Value}' disagrees with '{entry.UiLabel}'");
        }
    }

    [Test]
    public void Cli_verbs_are_shipped_in_the_cli_switch()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "ProjectAegis.MissionEditor.Cli", "Program.cs"));
        foreach (var entry in Entries.Where(e => e.CliVerb is not null))
        {
            Assert.That(program, Does.Contain($"case \"{entry.CliVerb}\":"), $"{entry.VerbId}: CLI verb '{entry.CliVerb}' missing");
        }
    }

    [Test]
    public void Source_gate_calls_match_declared_gate_methods()
    {
        foreach (var (verbId, files) in GateSources)
        {
            var entry = EditorVerbCatalog.Get(verbId);
            var calls = files
                .SelectMany(f => GateCall.Matches(File.ReadAllText(Path.Combine(RepoRoot(), f))).Select(m => m.Groups[1].Value))
                .ToHashSet(StringComparer.Ordinal);
            var sharedCliDeclared = Entries
                .Where(e => e.CliVerb is not null && e.CliVerb == entry.CliVerb)
                .SelectMany(e => e.GateMethods)
                .Concat(entry.GateMethods)
                .ToHashSet(StringComparer.Ordinal);

            Assert.That(calls, Is.SubsetOf(sharedCliDeclared), $"{verbId}: source calls gate methods its label does not admit");
            Assert.That(entry.GateMethods, Is.SubsetOf(calls), $"{verbId}: declared gate methods are not called in source");
        }
    }

    [Test]
    public void Platform_import_cli_proposes_only()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", "ProjectAegis.MissionEditor.Cli", "PlatformImportXlsxCommand.cs"));

        Assert.That(source, Does.Contain("ProposeFromFile"));
        Assert.That(source, Does.Not.Contain("Approve" + "Batch"));
        Assert.That(source, Does.Not.Contain("Reject" + "Batch"));
    }

    [Test]
    public void Propose_verb_at_runtime_only_calls_declared_propose_methods()
    {
        var source = Data();
        var edited = Edit(new PlatformWorkbookExporter().Export(source, "snap", new FixedCatalogClock(0)));
        var gate = new RecordingGate();
        var importer = new PlatformWorkbookImporter(id => id == "snap" ? source : null, new FixedCatalogClock(0));

        var result = importer.Stage(edited, gate, "human", "s125");

        Assert.That(result.Staged, Is.True);
        Assert.That(gate.Calls, Is.Not.Empty);
        Assert.That(gate.Calls, Is.SubsetOf(EditorVerbCatalog.Get("pe.propose").GateMethods));
        Assert.That(gate.Calls, Has.None.EqualTo("ApproveBatch").And.None.EqualTo("RejectBatch"));
    }

    [Test]
    public void Documented_verb_table_matches_catalog()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "engineering", "editor-verb-honesty.md"));
        var start = text.IndexOf("<!-- verbs:start -->", StringComparison.Ordinal);
        var end = text.IndexOf("<!-- verbs:end -->", StringComparison.Ordinal);
        Assert.That(start >= 0 && end > start, Is.True, "verb table markers missing");

        var documented = text[start..end].Split('\n')
            .Select(l => l.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray())
            .Where(c => c.Length >= 5 && c[0].StartsWith('`'))
            .Select(c => (Id: c[0].Trim('`'), Label: c[1], Cli: c[2].Trim('`'), Op: c[3]))
            .ToArray();

        Assert.That(documented.Select(d => d.Id), Is.EqualTo(Entries.Select(e => e.VerbId)));
        foreach (var d in documented)
        {
            var entry = EditorVerbCatalog.Get(d.Id);
            Assert.That(d.Label, Is.EqualTo(entry.UiLabel), d.Id);
            Assert.That(d.Cli, Is.EqualTo(entry.CliVerb ?? "—"), d.Id);
            Assert.That(d.Op, Is.EqualTo(entry.GateOperation.ToString()), d.Id);
        }
    }

    private static PlatformCatalogExportData Data() => new(
        Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
        Sensors: new[] { new CatalogSensorBinding("u1", "cmo-sensor-1", 0.85, ReviewState: CatalogReviewStates.Approved) },
        Mounts: new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
        Loadouts: new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
        Magazines: new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32) },
        Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
        Links: new[] { new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", LatencyMsNominal: 50) },
        Mobility: new[] { new CatalogMobility("u1", MaxSpeedKnots: 30) },
        Signatures: new[] { new CatalogSignature("u1", RcsBandDbsm: 10) },
        Emcon: new[] { new CatalogEmcon("u1", "silent", "radar-1") },
        Damage: new[] { new CatalogPlatformDamage("u1", 120, 25) },
        Swarms: new[] { new CatalogSwarmPlatform("u1", MaxDrones: 4) });

    private static PlatformWorkbook Edit(PlatformWorkbook workbook)
    {
        var edits = new Dictionary<string, (string Column, string Value)>(StringComparer.Ordinal)
        {
            ["Platforms"] = ("MaxHp", "150"),
            ["Sensors"] = ("BasePd", "0.5"),
            ["Mounts"] = ("Capacity", "40"),
            ["Loadouts"] = ("LoadoutName", "ASuW heavy"),
            ["Comms"] = ("SatcomCapable", "True"),
            ["LinkCatalog"] = ("LatencyMsNominal", "60"),
            ["Mobility"] = ("MaxSpeedKnots", "31"),
            ["Signatures"] = ("RcsBandDbsm", "11"),
            ["Emcon"] = ("Posture", "active"),
            ["Swarms"] = ("MaxDrones", "6"),
        };
        return new PlatformWorkbook(workbook.Sheets.Select(sheet =>
        {
            if (!edits.TryGetValue(sheet.Name, out var edit))
            {
                return sheet;
            }

            var col = sheet.Header.ToList().IndexOf(edit.Column);
            var rows = sheet.Rows.Select(r =>
            {
                var copy = r.ToArray();
                copy[col] = edit.Value;
                return (IReadOnlyList<string>)copy;
            }).ToArray();
            return sheet with { Rows = rows };
        }).ToArray());
    }

    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar)) ?? dir;
        }

        throw new DirectoryNotFoundException("ProjectAegis.sln not found above test output.");
    }

    private sealed class RecordingGate : IWriteGate
    {
        public List<string> Calls { get; } = new();

        private string Record(string name)
        {
            Calls.Add(name);
            return $"batch-{Calls.Count}";
        }

        public string ProposeSensorBatch(IReadOnlyList<CatalogSensorBinding> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeSensorBatch));
        public string ProposeMountBatch(IReadOnlyList<CatalogMount> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeMountBatch));
        public string ProposeLoadoutBatch(IReadOnlyList<CatalogLoadout> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeLoadoutBatch));
        public string ProposeMagazineBatch(IReadOnlyList<CatalogMagazineEntry> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeMagazineBatch));
        public string ProposeCommsBatch(IReadOnlyList<CatalogCommsBinding> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeCommsBatch));
        public string ProposeLinkCatalogBatch(IReadOnlyList<CatalogLinkEntry> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeLinkCatalogBatch));
        public string ProposePlatformBatch(IReadOnlyList<CatalogPlatformBinding> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposePlatformBatch));
        public string ProposeWeaponBatch(IReadOnlyList<CatalogWeaponRecord> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeWeaponBatch));
        public string ProposeMobilityBatch(IReadOnlyList<CatalogMobility> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeMobilityBatch));
        public string ProposeSignatureBatch(IReadOnlyList<CatalogSignature> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeSignatureBatch));
        public string ProposeEmconBatch(IReadOnlyList<CatalogEmcon> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeEmconBatch));
        public string ProposePlatformDamageBatch(IReadOnlyList<CatalogPlatformDamage> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposePlatformDamageBatch));
        public string ProposeSwarmBatch(IReadOnlyList<CatalogSwarmPlatform> proposed, string actorType, string actorId, string rationale = "") => Record(nameof(ProposeSwarmBatch));
        public WriteGateDecision ApproveBatch(string batchId, string actorType, string actorId) { Record(nameof(ApproveBatch)); return new WriteGateDecision(true, batchId, Array.Empty<string>()); }
        public WriteGateDecision RejectBatch(string batchId, string actorType, string actorId, string rationale = "") { Record(nameof(RejectBatch)); return new WriteGateDecision(false, batchId, Array.Empty<string>()); }
        public IReadOnlyList<CatalogStagingBatchSummary> ListPendingBatches() { Record(nameof(ListPendingBatches)); return Array.Empty<CatalogStagingBatchSummary>(); }
    }
}
