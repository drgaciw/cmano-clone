using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>S125-08 / AUTH-13 + AUTH-09: quarantine UX projection and lat/lon messaging for the PE import pane.</summary>
[TestFixture]
public sealed class PlatformImportQuarantineProjectionTests
{
    [Test]
    public void Empty_quarantine_says_so()
    {
        var state = PlatformImportQuarantineProjection.Bind(Array.Empty<PlatformImportQuarantineEntry>());

        Assert.That(state.HasQuarantine, Is.False);
        Assert.That(state.StatusLine, Is.EqualTo("QUARANTINE: empty"));
        Assert.That(state.HasSilentDrops, Is.False);
        Assert.That(state.NeverProposedNote, Does.Contain("never proposed"));
    }

    [Test]
    public void Rows_get_labels_actionable_hints_and_reason_counts()
    {
        var entries = new[]
        {
            new PlatformImportQuarantineEntry("mobility", "ghost", "", PlatformWorkbookValidator.PhaseBOrphanPlatform, "Mobility"),
            new PlatformImportQuarantineEntry("sensor", "u1", "s-low", "trl_below_minimum", "Sensors", "TrlLevel=2"),
            new PlatformImportQuarantineEntry("sensor", "u1", "s-prov", "review_state_provisional", "Sensors"),
            new PlatformImportQuarantineEntry("signature", "ghost", "", PlatformWorkbookValidator.PhaseBOrphanPlatform, "Signatures"),
        };

        var state = PlatformImportQuarantineProjection.Bind(entries);

        Assert.That(state.QuarantinedCount, Is.EqualTo(4));
        Assert.That(state.StatusLine, Is.EqualTo("QUARANTINE: 4 row(s) held across 3 reason(s) — not proposed"));
        Assert.That(state.Rows.Select(r => r.EntityKind), Is.Ordered);
        var orphan = state.Rows.First(r => r.ReasonCode == PlatformWorkbookValidator.PhaseBOrphanPlatform);
        Assert.That(orphan.ReasonLabel, Is.EqualTo("Unknown platform"));
        Assert.That(orphan.ActionHint, Does.Contain("Add PlatformId 'ghost'"));
        Assert.That(orphan.DisplayLine, Does.StartWith("QUARANTINE [mobility] ghost — Unknown platform"));
        Assert.That(orphan.UssClass, Does.Contain("platform-import-quarantine-row--mobility"));
        Assert.That(state.Rows.Single(r => r.EntityId == "s-low").ActionHint, Does.Contain("TrlLevel"));
        Assert.That(state.Rows.Single(r => r.EntityId == "s-prov").ActionHint, Does.Contain("approved"));
        var orphanCount = state.ReasonCounts.Single(c => c.ReasonCode == PlatformWorkbookValidator.PhaseBOrphanPlatform);
        Assert.That(orphanCount.Count, Is.EqualTo(2));
    }

    [Test]
    public void From_stage_result_carries_quarantine_drop_counts_and_latlon_messages()
    {
        var source = new PlatformCatalogExportData(
            Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
            Sensors: Array.Empty<CatalogSensorBinding>(),
            Mounts: Array.Empty<CatalogMount>(),
            Loadouts: Array.Empty<CatalogLoadout>(),
            Magazines: Array.Empty<CatalogMagazineEntry>(),
            Comms: Array.Empty<CatalogCommsBinding>(),
            Mobility: Array.Empty<CatalogMobility>());
        var exported = new PlatformWorkbookExporter().Export(source, "snap", new FixedCatalogClock(0));
        var edited = new PlatformWorkbook(exported.Sheets.Select(s => s.Name switch
        {
            "Mobility" => s with { Rows = new IReadOnlyList<string>[] { new[] { "ghost", "30", "18", "0", "0", "0", "0", "0" } } },
            "Platforms" => s with { Rows = s.Rows.Select(r => (IReadOnlyList<string>)r.Select((v, i) => i == 1 ? "123" : v).ToArray()).ToArray() },
            _ => s,
        }).ToArray());
        var importer = new PlatformWorkbookImporter(id => id == "snap" ? source : null, new FixedCatalogClock(0));

        var result = importer.Stage(edited, new NullGate(), "human", "s125");
        var state = PlatformImportQuarantineProjection.FromResult(result);

        Assert.That(state.HasQuarantine, Is.True);
        Assert.That(state.Rows.Select(r => r.PlatformId), Does.Contain("ghost"));
        Assert.That(state.HasSilentDrops, Is.True);
        Assert.That(state.DropSummaryLine, Does.Contain("quarantined row(s)"));
        Assert.That(state.LatLonLines, Has.Some.Contains("between -90 and 90"));
        Assert.That(state.LatLonLines, Has.Some.Contains("Mission Editor"));
    }

    private sealed class NullGate : IWriteGate
    {
        public string ProposeSensorBatch(IReadOnlyList<CatalogSensorBinding> p, string a, string i, string r = "") => "b";
        public string ProposeMountBatch(IReadOnlyList<CatalogMount> p, string a, string i, string r = "") => "b";
        public string ProposeLoadoutBatch(IReadOnlyList<CatalogLoadout> p, string a, string i, string r = "") => "b";
        public string ProposeMagazineBatch(IReadOnlyList<CatalogMagazineEntry> p, string a, string i, string r = "") => "b";
        public string ProposeCommsBatch(IReadOnlyList<CatalogCommsBinding> p, string a, string i, string r = "") => "b";
        public string ProposeLinkCatalogBatch(IReadOnlyList<CatalogLinkEntry> p, string a, string i, string r = "") => "b";
        public string ProposePlatformBatch(IReadOnlyList<CatalogPlatformBinding> p, string a, string i, string r = "") => "b";
        public string ProposeWeaponBatch(IReadOnlyList<CatalogWeaponRecord> p, string a, string i, string r = "") => "b";
        public string ProposeMobilityBatch(IReadOnlyList<CatalogMobility> p, string a, string i, string r = "") => "b";
        public string ProposeSignatureBatch(IReadOnlyList<CatalogSignature> p, string a, string i, string r = "") => "b";
        public string ProposeEmconBatch(IReadOnlyList<CatalogEmcon> p, string a, string i, string r = "") => "b";
        public string ProposePlatformDamageBatch(IReadOnlyList<CatalogPlatformDamage> p, string a, string i, string r = "") => "b";
        public string ProposeSwarmBatch(IReadOnlyList<CatalogSwarmPlatform> p, string a, string i, string r = "") => "b";
        public WriteGateDecision ApproveBatch(string b, string a, string i) => throw new InvalidOperationException();
        public WriteGateDecision RejectBatch(string b, string a, string i, string r = "") => throw new InvalidOperationException();
        public IReadOnlyList<CatalogStagingBatchSummary> ListPendingBatches() => Array.Empty<CatalogStagingBatchSummary>();
    }
}
