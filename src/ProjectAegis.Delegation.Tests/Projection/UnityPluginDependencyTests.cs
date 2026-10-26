using System.Security.Cryptography;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Prevents MCP and the game from loading different copies of shared dependencies.</summary>
public sealed class UnityPluginDependencyTests
{
    [Test]
    public void Shared_Unity_and_Mcp_dependencies_have_identical_binaries()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "ProjectAegis.sln")))
            root = root.Parent;

        Assert.That(root, Is.Not.Null, "Repository root must be available.");
        var plugins = Path.Combine(root!.FullName, "unity", "ProjectAegis", "Assets", "Plugins");
        var game = Path.Combine(plugins, "ProjectAegis");
        var nuget = Path.Combine(plugins, "NuGet");
        Assert.That(Directory.Exists(game), Is.True, "Publish the Unity plugins before testing.");
        if (!Directory.Exists(nuget))
            return; // MCP is optional on headless build agents.

        var mismatches = new List<string>();
        foreach (var dll in Directory.EnumerateFiles(game, "*.dll"))
        {
            var other = Path.Combine(nuget, Path.GetFileName(dll));
            if (!File.Exists(other))
                continue;

            using var gameStream = File.OpenRead(dll);
            using var mcpStream = File.OpenRead(other);
            if (!SHA256.HashData(gameStream).SequenceEqual(SHA256.HashData(mcpStream)))
                mismatches.Add(Path.GetFileName(dll));
        }

        Assert.That(mismatches, Is.Empty,
            "MCP can resolve excluded DLLs by assembly-qualified name. Importer exclusion alone " +
            "does not prevent mixed serializer types. Run tools/copy-delegation-assemblies.ps1. " +
            string.Join(", ", mismatches));
    }
}
