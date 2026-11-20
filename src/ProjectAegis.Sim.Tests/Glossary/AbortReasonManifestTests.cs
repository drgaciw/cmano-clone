using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Glossary;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

public sealed class AbortReasonManifestTests
{
    private static readonly string ManifestPath = ResolveManifestPath();

    [Test]
    public void Manifest_loads_and_maps_doctrine_codes()
    {
        var manifest = AbortReasonManifest.LoadFromEmbeddedOrFile(ManifestPath);
        Assert.That(manifest.TryGetLogCode("Doctrine", nameof(FireAbortReason.WraSalvo), out var code), Is.True);
        Assert.That(code, Is.EqualTo(AbortReasonCatalog.Doctrine.WRA_SALVO));
    }

    [Test]
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

            Assert.That(engageMembers.ContainsKey(reason.ToString()), Is.True);
            Assert.That(EngagementAbortReasonCodes.ToLogCode(reason), Is.EqualTo(manifest.GetLogCode("Engage", reason)));
        }
    }

    [Test]
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

            Assert.That(doctrineMembers.ContainsKey(reason.ToString()), Is.True);
        }
    }

    [Test]
    public void Logistics_codes_include_strike_unreachable_family()
    {
        var manifest = AbortReasonManifest.LoadFromEmbeddedOrFile(ManifestPath);
        Assert.That(manifest.LogisticsCodes, Does.Contain(AbortReasonCatalog.Logistics.STRIKE_UNREACHABLE));
        Assert.That(manifest.LogisticsCodes, Does.Contain(AbortReasonCatalog.Logistics.STRIKE_UNREACHABLE_FUEL));
    }

    [Test]
    public void Generated_catalog_matches_manifest_logistics_constants()
    {
        Assert.That(AbortReasonCatalog.Logistics.STRIKE_UNREACHABLE, Is.EqualTo("STRIKE_UNREACHABLE"));
        Assert.That(AbortReasonCatalog.Engage.BLACK_PROJECT_REQUIRED, Is.EqualTo("BLACK_PROJECT_REQUIRED"));
    }

    [Test]
    public void Sensor_and_cyber_string_families_align_with_manifest()
    {
        var manifest = AbortReasonManifest.LoadFromEmbeddedOrFile(ManifestPath);
        Assert.That(manifest.GetStringCodes("Sensor"), Does.Contain(AbortReasonCatalog.Sensor.DATALINK_STALE));
        Assert.That(manifest.GetStringCodes("Cyber"), Does.Contain(AbortReasonCatalog.Cyber.CYBER_ORDER_DELAY));
        Assert.That("TRACK_STALE", Is.EqualTo(AbortReasonCatalog.Sensor.TRACK_STALE));
        Assert.That("CYBER_LINK_DOWN", Is.EqualTo(AbortReasonCatalog.Cyber.CYBER_LINK_DOWN));
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