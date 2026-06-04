using Xunit;

namespace ProjectAegis.Sim.Tests.Glossary;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

public sealed class AbortReasonManifestTests
{
    private static readonly string ManifestPath = ResolveManifestPath();

    [Fact]
    public void Manifest_loads_and_maps_doctrine_codes()
    {
        var manifest = AbortReasonManifest.LoadFromEmbeddedOrFile(ManifestPath);
        Assert.True(manifest.TryGetLogCode("Doctrine", nameof(FireAbortReason.WraSalvo), out var code));
        Assert.Equal(AbortReasonCatalog.Doctrine.WRA_SALVO, code);
    }

    [Fact]
    public void Engagement_abort_reasons_align_with_manifest()
    {
        var manifest = AbortReasonManifest.LoadFromEmbeddedOrFile(ManifestPath);
        var engageMembers = manifest.GetFamilyMembers("Engage");
        foreach (EngagementAbortReason reason in Enum.GetValues<EngagementAbortReason>())
        {
            if (reason == EngagementAbortReason.None)
            {
                continue;
            }

            Assert.True(engageMembers.ContainsKey(reason.ToString()));
            Assert.Equal(manifest.GetLogCode("Engage", reason), EngagementAbortReasonCodes.ToLogCode(reason));
        }
    }

    [Fact]
    public void Fire_abort_reasons_align_with_manifest()
    {
        var manifest = AbortReasonManifest.LoadFromEmbeddedOrFile(ManifestPath);
        var doctrineMembers = manifest.GetFamilyMembers("Doctrine");
        foreach (FireAbortReason reason in Enum.GetValues<FireAbortReason>())
        {
            if (reason == FireAbortReason.None)
            {
                continue;
            }

            Assert.True(doctrineMembers.ContainsKey(reason.ToString()));
        }
    }

    [Fact]
    public void Logistics_codes_include_strike_unreachable_family()
    {
        var manifest = AbortReasonManifest.LoadFromEmbeddedOrFile(ManifestPath);
        Assert.Contains(AbortReasonCatalog.Logistics.STRIKE_UNREACHABLE, manifest.LogisticsCodes);
        Assert.Contains(AbortReasonCatalog.Logistics.STRIKE_UNREACHABLE_FUEL, manifest.LogisticsCodes);
    }

    [Fact]
    public void Generated_catalog_matches_manifest_logistics_constants()
    {
        Assert.Equal("STRIKE_UNREACHABLE", AbortReasonCatalog.Logistics.STRIKE_UNREACHABLE);
        Assert.Equal("BLACK_PROJECT_REQUIRED", AbortReasonCatalog.Engage.BLACK_PROJECT_REQUIRED);
    }

    private static string ResolveManifestPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "glossary", "abort_reason_manifest.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "glossary", "abort_reason_manifest.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException("abort_reason_manifest.json");
    }
}