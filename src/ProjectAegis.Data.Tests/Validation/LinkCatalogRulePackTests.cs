using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Validation;

/// <summary>S34-05 / Req-21 link-catalog detect-only rule pack.</summary>
public sealed class LinkCatalogRulePackTests
{
    [Test]
    public void Link_orphan_comms_flags_missing_link_catalog_entry()
    {
        var reader = BuildLinkReader(
            comms: [new CatalogCommsBinding("u1", "UNKNOWN_LINK")],
            links: CatalogValidationDefaults.BalticLinks());

        var findings = LinkCatalogRules.Evaluate(reader);

        Assert.That(findings.Any(f =>
            f.Code == LinkCatalogRules.OrphanCommsCode &&
            f.Severity == "error" &&
            f.Message == LinkCatalogRules.FormatOrphanCommsMessage("u1", "UNKNOWN_LINK")), Is.True);
    }

    [Test]
    public void Link_orphan_comms_passes_when_comms_resolves_to_catalog_entry()
    {
        var reader = BuildLinkReader(
            comms: [new CatalogCommsBinding("u1", "NATO_TADIL_J")],
            links: CatalogValidationDefaults.BalticLinks());

        var findings = LinkCatalogRules.Evaluate(reader);

        Assert.That(findings.Any(f => f.Code == LinkCatalogRules.OrphanCommsCode), Is.False);
    }

    [Test]
    public void Link_type_invalid_flags_unknown_link_type()
    {
        var reader = BuildLinkReader(
            links:
            [
                new CatalogLinkEntry("BAD_TYPE_LINK", "Bad Type", "hf-radio", LatencyMsNominal: 50),
            ]);

        var findings = LinkCatalogRules.Evaluate(reader);

        Assert.That(findings.Any(f =>
            f.Code == LinkCatalogRules.TypeInvalidCode &&
            f.Severity == "error" &&
            f.Message == LinkCatalogRules.FormatTypeInvalidMessage("BAD_TYPE_LINK", "hf-radio")), Is.True);
    }
    [TestCase(CatalogLinkTypes.Strategic)]
    [TestCase(CatalogLinkTypes.Tactical)]
    [TestCase(CatalogLinkTypes.Voice)]
    [TestCase(CatalogLinkTypes.Satcom)]
    public void Link_type_invalid_passes_for_allowed_catalog_link_types(string linkType)
    {
        var reader = BuildLinkReader(
            links: [new CatalogLinkEntry("VALID_TYPE", "Valid", linkType, LatencyMsNominal: 50)]);

        var findings = LinkCatalogRules.Evaluate(reader);

        Assert.That(findings.Any(f => f.Code == LinkCatalogRules.TypeInvalidCode), Is.False);
    }

    [Test]
    public void Link_latency_invalid_flags_negative_latency()
    {
        var reader = BuildLinkReader(
            links: [new CatalogLinkEntry("NEG_LATENCY", "Negative", CatalogLinkTypes.Tactical, LatencyMsNominal: -1)]);

        var findings = LinkCatalogRules.Evaluate(reader);

        Assert.That(findings.Any(f =>
            f.Code == LinkCatalogRules.LatencyInvalidCode &&
            f.Severity == "error" &&
            f.Message == LinkCatalogRules.FormatLatencyInvalidMessage("NEG_LATENCY", -1)), Is.True);
    }

    [Test]
    public void Link_latency_invalid_flags_latency_above_max()
    {
        var reader = BuildLinkReader(
            links:
            [
                new CatalogLinkEntry(
                    "HIGH_LATENCY",
                    "Too Slow",
                    CatalogLinkTypes.Strategic,
                    LatencyMsNominal: LinkCatalogRules.MaxLatencyMsNominal + 1),
            ]);

        var findings = LinkCatalogRules.Evaluate(reader);

        Assert.That(findings.Any(f =>
            f.Code == LinkCatalogRules.LatencyInvalidCode &&
            f.Severity == "error" &&
            f.Message == LinkCatalogRules.FormatLatencyInvalidMessage(
                "HIGH_LATENCY",
                LinkCatalogRules.MaxLatencyMsNominal + 1)), Is.True);
    }
    [TestCase(0)]
    [TestCase(50)]
    [TestCase(300_000)]
    public void Link_latency_invalid_passes_within_bounds(int latencyMsNominal)
    {
        var reader = BuildLinkReader(
            links:
            [
                new CatalogLinkEntry("BOUNDARY_OK", "Boundary", CatalogLinkTypes.Tactical, latencyMsNominal),
            ]);

        var findings = LinkCatalogRules.Evaluate(reader);

        Assert.That(findings.Any(f => f.Code == LinkCatalogRules.LatencyInvalidCode), Is.False);
    }

    [Test]
    public void Link_Baltic_patrol_golden_hash_stable_on_clean_fixture()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        var findings = LinkCatalogRules.Evaluate(reader);

        Assert.That(findings, Is.Empty);
        Assert.That(LinkCatalogRules.ComputeFindingsHash(findings), Is.EqualTo(LinkCatalogGoldenHashes.BalticPatrolClean));
        Assert.That(            LinkCatalogRules.ComputeFindingsHash(LinkCatalogRules.Evaluate(reader)), Is.EqualTo(            LinkCatalogRules.ComputeFindingsHash(findings)));
    }

    [Test]
    public void Link_findings_are_deterministically_sorted()
    {
        var reader = BuildLinkReader(
            comms:
            [
                new CatalogCommsBinding("u2", "MISSING_B"),
                new CatalogCommsBinding("u1", "MISSING_A"),
            ],
            links:
            [
                new CatalogLinkEntry("Z_LINK", "Z", "bad-type", LatencyMsNominal: -5),
                new CatalogLinkEntry("A_LINK", "A", CatalogLinkTypes.Tactical, LatencyMsNominal: 999_999),
            ]);

        var first = LinkCatalogRules.Evaluate(reader);
        var second = LinkCatalogRules.Evaluate(reader);

        Assert.That(second.Count, Is.EqualTo(first.Count));
        for (var i = 0; i < first.Count; i++)
        {
            Assert.That(second[i], Is.EqualTo(first[i]));
        }

        Assert.That(first
            .Zip(first.Skip(1))
            .All(pair => string.Compare(pair.First.Code, pair.Second.Code, StringComparison.Ordinal) <= 0), Is.True);
    }

    [Test]
    public void CatalogRulesValidationAgent_appends_link_findings_after_kill_chain()
    {
        var reader = BuildLinkReader(
            comms: [new CatalogCommsBinding("u1", "GHOST_LINK")],
            links: CatalogValidationDefaults.BalticLinks());

        var report = new CatalogRulesValidationAgent().Run(new DatabaseAgentContext(reader));

        Assert.That(report.Passed, Is.False);
        Assert.That(report.Findings.Any(f => f.Code == LinkCatalogRules.OrphanCommsCode), Is.True);
        Assert.That(report.Findings.Any(f => f.Code.StartsWith("LINK_", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void CrossSystem_orchestrator_Baltic_default_has_no_link_codes_on_clean_catalog()
    {
        var result = DatabaseIntelligenceOrchestrator.RunBalticDefault();
        var rules = result.Reports.First(r => r.AgentId == "rules_validation");

        Assert.That(rules.Findings.Any(f => f.Code.StartsWith("LINK_", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void CrossSystem_orchestrator_surfaces_link_codes_on_curated_fixture()
    {
        var reader = BuildLinkReader(
            comms: [new CatalogCommsBinding("u1", "GHOST_LINK")],
            links:
            [
                new CatalogLinkEntry("BAD_LINK", "Bad", "hf-radio", LatencyMsNominal: -1),
            ]);

        var result = new DatabaseIntelligenceOrchestrator().Run(reader);
        var rules = result.Reports.First(r => r.AgentId == "rules_validation");
        var linkCodes = rules.Findings
            .Where(f => f.Code.StartsWith("LINK_", StringComparison.Ordinal))
            .Select(f => f.Code)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

        Assert.That(linkCodes.Length, Is.EqualTo(3));
        Assert.That(linkCodes.Any(c => c == LinkCatalogRules.OrphanCommsCode), Is.True);
        Assert.That(linkCodes.Any(c => c == LinkCatalogRules.TypeInvalidCode), Is.True);
        Assert.That(linkCodes.Any(c => c == LinkCatalogRules.LatencyInvalidCode), Is.True);
    }

    [Test]
    public void Link_validation_messages_stable_on_curated_invalid_fixture()
    {
        var reader = BuildLinkReader(
            comms: [new CatalogCommsBinding("u1", "GHOST_LINK")],
            links:
            [
                new CatalogLinkEntry("BAD_LINK", "Bad", "hf-radio", LatencyMsNominal: -1),
            ]);

        var findings = LinkCatalogRules.Evaluate(reader);
        var byCode = findings.ToDictionary(f => f.Code, StringComparer.Ordinal);

        Assert.That(            byCode[LinkCatalogRules.OrphanCommsCode].Message, Is.EqualTo(            LinkCatalogRules.FormatOrphanCommsMessage("u1", "GHOST_LINK")));
        Assert.That(            byCode[LinkCatalogRules.TypeInvalidCode].Message, Is.EqualTo(            LinkCatalogRules.FormatTypeInvalidMessage("BAD_LINK", "hf-radio")));
        Assert.That(            byCode[LinkCatalogRules.LatencyInvalidCode].Message, Is.EqualTo(            LinkCatalogRules.FormatLatencyInvalidMessage("BAD_LINK", -1)));

        var rerun = LinkCatalogRules.Evaluate(reader);
        Assert.That(rerun.Count, Is.EqualTo(findings.Count));
        for (var i = 0; i < findings.Count; i++)
        {
            Assert.That(rerun[i], Is.EqualTo(findings[i]));
        }
    }

    [Test]
    public void CatalogRules_detect_only_does_not_mutate_catalog_on_link_report_path()
    {
        var reader = BuildLinkReader(
            comms: [new CatalogCommsBinding("u1", "GHOST_LINK")],
            links:
            [
                new CatalogLinkEntry("BAD_LINK", "Bad", "hf-radio", LatencyMsNominal: -1),
            ]);

        var commsBefore = reader.GetSortedComms().Count;
        var linksBefore = reader.GetSortedLinks().Count;

        _ = new CatalogRulesValidationAgent().Run(new DatabaseAgentContext(reader));

        Assert.That(reader.GetSortedComms().Count, Is.EqualTo(commsBefore));
        Assert.That(reader.GetSortedLinks().Count, Is.EqualTo(linksBefore));
    }

    private static InMemoryCatalogReader BuildLinkReader(
        IEnumerable<CatalogCommsBinding>? comms = null,
        IEnumerable<CatalogLinkEntry>? links = null) =>
        new(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, ReviewState: CatalogReviewStates.Approved)],
            "link-catalog-fixture",
            CatalogValidationDefaults.BalticPlatforms(),
            comms: comms ?? [],
            links: links ?? CatalogValidationDefaults.BalticLinks());
}