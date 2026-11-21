using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Packaging;

/// <summary>
/// Guards every committed Unity <c>.meta</c> file under <c>unity/ProjectAegis/Assets</c>
/// against a malformed <c>guid:</c> line. Unity ignores an asset whose GUID is not 32 lowercase
/// hex characters and logs "does not have a valid GUID", which breaks UXML/USS loading at runtime.
/// </summary>
public sealed class UnityMetaGuidValidityTests
{
    private static readonly Regex GuidLine = new(@"^guid:\s*(\S*)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex ValidGuid = new(@"^[0-9a-f]{32}$", RegexOptions.CultureInvariant);

    [Test]
    public void Every_unity_meta_file_has_a_valid_32_hex_guid()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var assets = Path.Combine(repoRoot!, "unity", "ProjectAegis", "Assets");
        Assert.That(Directory.Exists(assets), Is.True, assets);

        var invalid = new List<string>();
        foreach (var meta in Directory.EnumerateFiles(assets, "*.meta", SearchOption.AllDirectories))
        {
            var match = GuidLine.Match(File.ReadAllText(meta));
            var guid = match.Success ? match.Groups[1].Value : string.Empty;
            if (!ValidGuid.IsMatch(guid))
            {
                invalid.Add($"{Path.GetRelativePath(repoRoot!, meta)} (guid='{guid}')");
            }
        }

        Assert.That(
            invalid,
            Is.Empty,
            "Unity ignores assets whose .meta GUID is not 32 lowercase hex characters:\n" + string.Join("\n", invalid));
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
