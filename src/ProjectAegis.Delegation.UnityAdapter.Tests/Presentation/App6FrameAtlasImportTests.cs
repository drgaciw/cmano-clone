using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// The APP-6 frame atlas is a 112x16 strip of seven 16 px frames. <c>MapPlaceholderPanel.uss</c>
/// selects a frame with <c>background-position-x</c> over the whole PNG. That only works when the PNG
/// imports as a single sprite: with <c>spriteMode: 2</c> (Multiple) Unity resolves the <c>url()</c> to
/// the first sub-sprite (the blue friendly frame), so every affiliation renders friendly-blue
/// (UAT D4b: hostile-1 drew as a blue square while its label was hostile-red).
/// </summary>
public sealed class App6FrameAtlasImportTests
{
    private static readonly Regex SpriteMode =
        new(@"^\s*spriteMode:\s*(\d+)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    [Test]
    public void Atlas_png_imports_as_a_single_sprite_because_uss_offsets_assume_the_whole_texture()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var dir = Path.Combine(repoRoot!, "unity", "ProjectAegis", "Assets", "UI", "MapPlaceholder");
        var meta = File.ReadAllText(Path.Combine(dir, "App6FrameAtlas.png.meta"));
        var uss = File.ReadAllText(Path.Combine(dir, "MapPlaceholderPanel.uss"));

        // Precondition: the USS really selects frames by offset over the whole PNG.
        Assert.That(
            uss,
            Does.Contain("url(\"project://database/Assets/UI/MapPlaceholder/App6FrameAtlas.png\")"),
            "USS no longer references the whole atlas PNG; revisit this guard.");
        Assert.That(uss, Does.Contain("background-position-x: -16px"), "Hostile frame offset missing from USS.");

        var mode = SpriteMode.Match(meta);
        Assert.That(mode.Success, Is.True, "App6FrameAtlas.png.meta has no spriteMode.");
        Assert.That(
            mode.Groups[1].Value,
            Is.EqualTo("1"),
            "Atlas must import as Single (spriteMode: 1); Multiple resolves url() to the first (friendly) sub-sprite.");
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
