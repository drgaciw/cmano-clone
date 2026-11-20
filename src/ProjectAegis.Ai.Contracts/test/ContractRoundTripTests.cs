using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace ProjectAegis.Ai.Contracts.Tests;

/// <summary>Deterministic JSON round-trips with fixed property order (declaration order).</summary>
[TestFixture]
public sealed class ContractRoundTripTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
    };

    [Test]
    public void Schema_version_is_current()
    {
        Assert.That(AiContractSchema.Version, Is.EqualTo(2));
        Assert.That(AiContractSchema.VersionString, Is.EqualTo("2"));
    }

    [Test]
    public void AdviceExplanationContent_round_trip_preserves_property_order()
    {
        var content = new AdviceExplanationContent(
            InvocationKey: "c2.advice.datalink.assess:1:1.5:hostile-1",
            EvidenceFingerprint: "ev-fp-1",
            ViewingSide: ViewingSide.Blue,
            Explanation: "Contact remains advisory-only.",
            CitedFactKeys: new[] { "rationale", "hardConstraints" },
            PromptVersion: "advice-explain/v1");

        RoundTripAndAssertOrder(
            content,
            "InvocationKey",
            "EvidenceFingerprint",
            "ViewingSide",
            "Explanation",
            "CitedFactKeys",
            "PromptVersion");
    }

    [Test]
    public void EvidenceId_format_is_prefix_colon_sequence()
    {
        Assert.That(EvidenceId.From("abc123", 42), Is.EqualTo("abc123:42"));
    }

    [Test]
    public void EvidenceHeader_round_trip_preserves_property_order()
    {
        var header = new EvidenceHeader(
            SchemaVersion: AiContractSchema.Version,
            BuildIdentity: "build-1",
            ScenarioId: "baltic-v3",
            CatalogRevision: "cat-9",
            OrderLogFingerprint: "fpdeadbeef",
            ViewingSide: ViewingSide.Blue,
            DebriefMode: false,
            EvidenceIds: new[] { "fpdeadbeef:1", "fpdeadbeef:2" });

        RoundTripAndAssertOrder(
            header,
            "SchemaVersion",
            "BuildIdentity",
            "ScenarioId",
            "CatalogRevision",
            "OrderLogFingerprint",
            "ViewingSide",
            "DebriefMode",
            "EvidenceIds");
    }

    [Test]
    public void EvidenceEntry_round_trip_preserves_property_order()
    {
        var entry = new EvidenceEntry(
            EvidenceId: "fpdeadbeef:7",
            SequenceId: 7,
            Kind: EvidenceEntryKind.PolicyDenial,
            SimTime: 12.5,
            ViewingSide: ViewingSide.Blue,
            UnitId: "u1",
            Summary: "denied",
            Attributes: new Dictionary<string, string> { ["reason"] = "roe" });

        RoundTripAndAssertOrder(
            entry,
            "EvidenceId",
            "SequenceId",
            "Kind",
            "SimTime",
            "ViewingSide",
            "UnitId",
            "Summary",
            "Attributes");
    }

    [Test]
    public void EvidenceDocument_round_trip()
    {
        var doc = new EvidenceDocument(
            new EvidenceHeader(
                AiContractSchema.Version,
                "build-1",
                "scenario-a",
                "cat-1",
                "fp1",
                ViewingSide.Red,
                DebriefMode: true,
                EvidenceIds: new[] { "fp1:1" }),
            new[]
            {
                new EvidenceEntry("fp1:1", 1, EvidenceEntryKind.Engagement, 1.0, ViewingSide.Red, "hostile-1", "hit", null),
            });

        var json = JsonSerializer.Serialize(doc, Options);
        var again = JsonSerializer.Deserialize<EvidenceDocument>(json, Options);
        Assert.That(again, Is.Not.Null);
        Assert.That(JsonSerializer.Serialize(again, Options), Is.EqualTo(json));
    }

    [Test]
    public void ArtifactEnvelope_round_trip_preserves_property_order()
    {
        var envelope = new ArtifactEnvelope(
            SchemaVersion: AiContractSchema.Version,
            BuildIdentity: "build-1",
            ScenarioId: "scenario-a",
            CatalogRevision: "cat-1",
            OrderLogFingerprint: "fp1",
            ProviderId: "fake",
            ModelId: "fake-v1",
            PromptVersion: "aar-explain-1",
            EvidenceIds: new[] { "fp1:1" },
            ViewingSide: ViewingSide.Blue,
            DebriefMode: false,
            Status: GenerationStatus.Succeeded,
            ContentHash: "sha256:abc");

        RoundTripAndAssertOrder(
            envelope,
            "SchemaVersion",
            "BuildIdentity",
            "ScenarioId",
            "CatalogRevision",
            "OrderLogFingerprint",
            "ProviderId",
            "ModelId",
            "PromptVersion",
            "EvidenceIds",
            "ViewingSide",
            "DebriefMode",
            "Status",
            "ContentHash");
    }

    [Test]
    public void AiArtifactTransport_round_trip()
    {
        var transport = new AiArtifactTransport(
            new ArtifactEnvelope(
                AiContractSchema.Version,
                "build-1",
                "scenario-a",
                "cat-1",
                "fp1",
                "fake",
                "fake-v1",
                "aar-explain-1",
                new[] { "fp1:1" },
                ViewingSide.Blue,
                false,
                GenerationStatus.TimedOut,
                "sha256:abc"),
            ContentMediaType: "application/json",
            ContentText: "{\"ok\":false}",
            Warnings: new[] { "timeout" });

        var json = JsonSerializer.Serialize(transport, Options);
        var again = JsonSerializer.Deserialize<AiArtifactTransport>(json, Options);
        Assert.That(again, Is.Not.Null);
        Assert.That(JsonSerializer.Serialize(again, Options), Is.EqualTo(json));
    }

    [TestCase(GenerationStatus.Succeeded)]
    [TestCase(GenerationStatus.TimedOut)]
    [TestCase(GenerationStatus.Cancelled)]
    [TestCase(GenerationStatus.MalformedOutput)]
    [TestCase(GenerationStatus.ProviderUnavailable)]
    [TestCase(GenerationStatus.RejectedCitation)]
    [TestCase(GenerationStatus.RejectedNumber)]
    public void GenerationStatus_round_trips(GenerationStatus status)
    {
        var json = JsonSerializer.Serialize(status, Options);
        var again = JsonSerializer.Deserialize<GenerationStatus>(json, Options);
        Assert.That(again, Is.EqualTo(status));
    }

    private static void RoundTripAndAssertOrder<T>(T value, params string[] expectedPropertyOrder)
    {
        var json = JsonSerializer.Serialize(value, Options);
        var again = JsonSerializer.Deserialize<T>(json, Options);
        Assert.That(again, Is.Not.Null);
        // Compare via JSON so IReadOnlyList array vs List deserializations still match.
        Assert.That(JsonSerializer.Serialize(again, Options), Is.EqualTo(json));

        var node = JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidOperationException("Expected JSON object.");
        var actual = node.Select(p => p.Key).ToArray();
        Assert.That(actual, Is.EqualTo(expectedPropertyOrder));
    }
}
