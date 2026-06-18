using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.App6;

/// <summary>
/// Headless contract tests for S26-06 APP-6 texture atlas asset pack (ADR-007 Phase C).
/// </summary>
public sealed class App6AtlasAssetTests
{
    [Test]
    public void KnownUssFrameIds_covers_seven_distinct_affiliation_frames()
    {
        Assert.That(App6Sidc.KnownUssFrameIds, Has.Count.EqualTo(7));
        Assert.That(App6Sidc.KnownUssFrameIds, Is.Unique);
        Assert.That(App6Sidc.KnownUssFrameIds, Does.Contain(App6Sidc.NeutralUnitFrame));
        Assert.That(App6Sidc.KnownUssFrameIds, Does.Contain(App6Sidc.SuspectContactFrame));
        Assert.That(App6Sidc.KnownUssFrameIds, Does.Contain(App6Sidc.PendingUnitFrame));
    }

    [Test]
    public void SpriteSheet_has_slice_for_every_known_frame_id()
    {
        foreach (var frameId in App6Sidc.KnownUssFrameIds)
        {
            Assert.That(App6AtlasSpriteSheet.TryGetSlice(frameId, out var slice), Is.True, frameId);
            Assert.That(slice.Width, Is.EqualTo(App6AtlasSpriteSheet.FrameWidth));
            Assert.That(slice.Height, Is.EqualTo(App6AtlasSpriteSheet.FrameHeight));
        }
    }

    [Test]
    public void SpriteSheet_slices_are_distinct_for_friendly_hostile_neutral_and_suspect()
    {
        Assert.That(App6AtlasSpriteSheet.TryGetSlice(App6Sidc.FriendlySurfaceUnitFrame, out var friendly), Is.True);
        Assert.That(App6AtlasSpriteSheet.TryGetSlice(App6Sidc.HostileContactFrame, out var hostile), Is.True);
        Assert.That(App6AtlasSpriteSheet.TryGetSlice(App6Sidc.NeutralUnitFrame, out var neutral), Is.True);
        Assert.That(App6AtlasSpriteSheet.TryGetSlice(App6Sidc.SuspectContactFrame, out var suspect), Is.True);

        Assert.That(friendly, Is.Not.EqualTo(hostile));
        Assert.That(friendly, Is.Not.EqualTo(neutral));
        Assert.That(hostile, Is.Not.EqualTo(suspect));
        Assert.That(neutral.X, Is.EqualTo(64));
        Assert.That(suspect.X, Is.EqualTo(80));
    }

    [Test]
    public void Default_catalog_exposes_sprite_slices_for_registered_frames()
    {
        var catalog = App6AtlasCatalog.Default;

        Assert.That(catalog.TryGetSpriteSlice(App6Sidc.FriendlySurfaceUnitFrame, out var friendly), Is.True);
        Assert.That(catalog.TryGetSpriteSlice(App6Sidc.PendingUnitFrame, out var pending), Is.True);
        Assert.That(friendly.X, Is.EqualTo(0));
        Assert.That(pending.X, Is.EqualTo(96));
    }

    [Test]
    public void Unavailable_catalog_does_not_expose_sprite_slices()
    {
        var catalog = App6AtlasCatalog.Unavailable;

        Assert.That(catalog.TryGetSpriteSlice(App6Sidc.FriendlySurfaceUnitFrame, out _), Is.False);
    }

    [Test]
    public void ResolveMapGlyph_expanded_affiliations_return_distinct_frames_and_glyphs()
    {
        var friendly = App6Sidc.ResolveMapGlyph("Friendly");
        var hostile = App6Sidc.ResolveMapGlyph("Hostile");
        var neutral = App6Sidc.ResolveMapGlyph("Neutral");
        var suspect = App6Sidc.ResolveMapGlyph("Suspect");
        var pending = App6Sidc.ResolveMapGlyph("Pending");

        var frameIds = new[]
        {
            friendly.UssFrameId,
            hostile.UssFrameId,
            neutral.UssFrameId,
            suspect.UssFrameId,
            pending.UssFrameId,
        };
        Assert.That(frameIds, Is.Unique);

        var glyphs = new[]
        {
            friendly.UnicodeGlyph,
            hostile.UnicodeGlyph,
            neutral.UnicodeGlyph,
            suspect.UnicodeGlyph,
            pending.UnicodeGlyph,
        };
        Assert.That(glyphs, Is.Unique);
    }

    [Test]
    public void ResolveMapGlyphFromSidc_parses_neutral_suspect_and_pending_identities()
    {
        var neutral = App6Sidc.ResolveMapGlyphFromSidc(App6Sidc.NeutralUnitSidc);
        var suspect = App6Sidc.ResolveMapGlyphFromSidc(App6Sidc.SuspectContactSidc);
        var pending = App6Sidc.ResolveMapGlyphFromSidc(App6Sidc.PendingUnitSidc);

        Assert.That(neutral.UssFrameId, Is.EqualTo(App6Sidc.NeutralUnitFrame));
        Assert.That(suspect.UssFrameId, Is.EqualTo(App6Sidc.SuspectContactFrame));
        Assert.That(pending.UssFrameId, Is.EqualTo(App6Sidc.PendingUnitFrame));
    }

    [Test]
    public void ResolveDisplay_with_texture_atlas_catalog_uses_sprite_backed_frames()
    {
        var neutral = App6GlyphAtlas.ResolveDisplay("Neutral", atlas: App6AtlasCatalog.Default);
        var suspect = App6GlyphAtlas.ResolveDisplay("Suspect", atlas: App6AtlasCatalog.Default);

        Assert.That(neutral.UsesAtlasFrame, Is.True);
        Assert.That(suspect.UsesAtlasFrame, Is.True);
        Assert.That(neutral.AtlasFrameClass, Is.EqualTo(App6Sidc.NeutralUnitFrame));
        Assert.That(suspect.AtlasFrameClass, Is.EqualTo(App6Sidc.SuspectContactFrame));
        Assert.That(neutral.Glyph, Is.Empty);
        Assert.That(suspect.Glyph, Is.Empty);
    }

    [Test]
    public void Addressables_manifest_and_texture_assets_exist_in_repo()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var texturePath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "MapPlaceholder",
            "App6FrameAtlas.png");
        var manifestPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Addressables",
            "Map",
            "App6AtlasAddressablesManifest.json");
        var ussPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "MapPlaceholder",
            "MapPlaceholderPanel.uss");

        Assert.That(File.Exists(texturePath), Is.True);
        Assert.That(File.Exists(manifestPath), Is.True);
        Assert.That(File.Exists(ussPath), Is.True);

        var manifest = File.ReadAllText(manifestPath);
        Assert.That(manifest, Does.Contain(App6AtlasSpriteSheet.AddressableKey));
        Assert.That(manifest, Does.Contain(App6AtlasSpriteSheet.TextureAssetPath));

        var uss = File.ReadAllText(ussPath);
        Assert.That(uss, Does.Contain("App6FrameAtlas.png"));
        Assert.That(uss, Does.Contain(App6Sidc.NeutralUnitFrame));
        Assert.That(uss, Does.Contain("background-size: 112px 16px"));
        Assert.That(uss, Does.Not.Contain("placeholder vector frames"));
    }

    [Test]
    public void DelegationBridge_cs_has_zero_diff_for_app6_atlas_story()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var bridgePath = Path.Combine(
            repoRoot!,
            "src",
            "ProjectAegis.Delegation.UnityAdapter",
            "Bridge",
            "DelegationBridge.cs");
        Assert.That(File.Exists(bridgePath), Is.True);

        var source = File.ReadAllText(bridgePath);
        Assert.That(source, Does.Not.Contain("App6AtlasSpriteSheet"));
        Assert.That(source, Does.Not.Contain("App6FrameAtlas"));
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}