using System.Text.Json.Serialization;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>
/// Source-generated JSON metadata for evidence JSONL (fixed property order, compact, LF).
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(EvidenceHeader))]
[JsonSerializable(typeof(EvidenceEntry))]
[JsonSerializable(typeof(EvidenceDocument))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class EvidenceJsonlJsonContext : JsonSerializerContext;
