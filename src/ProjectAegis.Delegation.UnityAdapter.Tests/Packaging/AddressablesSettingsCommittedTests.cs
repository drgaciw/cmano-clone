using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Packaging;

/// <summary>
/// Guards the committed Addressables settings (UAT D35): the generated
/// <c>AddressableAssetsData</c> folder must exist and its <c>MapPresentation</c> group must register the
/// APP-6 frame atlas under the address <c>Map/App6FrameAtlas</c>.
/// </summary>
public sealed class AddressablesSettingsCommittedTests
{
    private const string AtlasAddress = "Map/App6FrameAtlas";
    private static readonly Regex GuidLine = new(@"^guid:\s*([0-9a-f]{32})\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    [Test]
    public void Addressables_settings_asset_is_committed()
    {
        var settings = Path.Combine(AssetsRoot(), "AddressableAssetsData", "AddressableAssetSettings.asset");
        Assert.That(File.Exists(settings), Is.True, "Missing committed Addressables settings: " + settings);
    }

    [Test]
    public void Asset_group_registers_app6_atlas_guid_and_address()
    {
        var assets = AssetsRoot();
        var atlasMeta = Path.Combine(assets, "UI", "MapPlaceholder", "App6FrameAtlas.png.meta");
        Assert.That(File.Exists(atlasMeta), Is.True, atlasMeta);
        var match = GuidLine.Match(File.ReadAllText(atlasMeta));
        Assert.That(match.Success, Is.True, "No 32-hex guid in " + atlasMeta);
        var atlasGuid = match.Groups[1].Value;

        var groups = Path.Combine(assets, "AddressableAssetsData", "AssetGroups");
        Assert.That(Directory.Exists(groups), Is.True, "Missing " + groups);

        var hit = Directory.EnumerateFiles(groups, "*.asset", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .Any(text => text.Contains(atlasGuid, StringComparison.Ordinal)
                      && text.Contains(AtlasAddress, StringComparison.Ordinal));
        Assert.That(hit, Is.True, $"No asset group references guid {atlasGuid} with address {AtlasAddress}");
    }

    private static string AssetsRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return Path.Combine(dir, "unity", "ProjectAegis", "Assets");
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        Assert.Fail("Repo root (ProjectAegis.sln) not found");
        return string.Empty;
    }
}
