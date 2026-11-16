using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace ProjectAegis.Ai.Contracts.Tests;

/// <summary>Deterministic JSON round-trips with fixed property order (declaration order).</summary>
public sealed class ContractRoundTripTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
    };

    [Fact]
    public void Schema_version_is_current()
    {
        Assert.Equal(2, AiContractSchema.Version);
        Assert.Equal("2", AiContractSchema.VersionString);
    }

    [Fact]
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

    [Fact]
    public void EvidenceId_format_is_prefix_colon_sequence()
    {
        Assert.Equal("abc123:42", EvidenceId.From("abc123", 42));
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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
        Assert.NotNull(again);
        Assert.Equal(json, JsonSerializer.Serialize(again, Options));
    }

    [Fact]
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

    [Fact]
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
        Assert.NotNull(again);
        Assert.Equal(json, JsonSerializer.Serialize(again, Options));
    }

    [Theory]
    [InlineData(GenerationStatus.Succeeded)]
    [InlineData(GenerationStatus.TimedOut)]
    [InlineData(GenerationStatus.Cancelled)]
    [InlineData(GenerationStatus.MalformedOutput)]
    [InlineData(GenerationStatus.ProviderUnavailable)]
    [InlineData(GenerationStatus.RejectedCitation)]
    [InlineData(GenerationStatus.RejectedNumber)]
    public void GenerationStatus_round_trips(GenerationStatus status)
    {
        var json = JsonSerializer.Serialize(status, Options);
        var again = JsonSerializer.Deserialize<GenerationStatus>(json, Options);
        Assert.Equal(status, again);
    }

    private static void RoundTripAndAssertOrder<T>(T value, params string[] expectedPropertyOrder)
    {
        var json = JsonSerializer.Serialize(value, Options);
        var again = JsonSerializer.Deserialize<T>(json, Options);
        Assert.NotNull(again);
        // Compare via JSON so IReadOnlyList array vs List deserializations still match.
        Assert.Equal(json, JsonSerializer.Serialize(again, Options));

        var node = JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidOperationException("Expected JSON object.");
        var actual = node.Select(p => p.Key).ToArray();
        Assert.Equal(expectedPropertyOrder, actual);
    }
}
