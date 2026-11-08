using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// Guardrail: Unity Runtime hosts resolve Epic A types from the precompiled
/// <c>Assets/Plugins/ProjectAegis/ProjectAegis.Delegation.dll</c> (not from live source).
/// Stale plugins force Unity Safe Mode (CS0246 / CS0234).
/// </summary>
public sealed class UnityPluginEpicATypesTests
{
    private static readonly string[] RequiredTypeFullNames =
    {
        "ProjectAegis.Delegation.Projection.C2TopBarPresentation",
        "ProjectAegis.Delegation.Projection.C2TopBarApplyState",
        "ProjectAegis.Delegation.Projection.LeftDrawerPresentation",
        "ProjectAegis.Delegation.Projection.LeftDrawerApplyState",
        "ProjectAegis.Delegation.Projection.UnitDetailPresentation",
        "ProjectAegis.Delegation.Projection.UnitDetailApplyState",
        "ProjectAegis.Delegation.Projection.CombatDomainsHotTickPanelState",
        "ProjectAegis.Delegation.Projection.CombatDomainsHotTickPanelBinder",
        "ProjectAegis.Delegation.Projection.ApprovedC2AssetPaths",
        "ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths",
        "ProjectAegis.Delegation.Projection.MapPanelApplyState",
        "ProjectAegis.Delegation.Projection.MapCanvasOverlayGeometry",
        "ProjectAegis.Delegation.Projection.MapEnvelopePlatformResolver",
        "ProjectAegis.Delegation.Projection.MapCanvasRingShape",
        "ProjectAegis.Delegation.Projection.MapCanvasEdgeShape",
        "ProjectAegis.Delegation.Projection.MapCourseOverlayEntry",
    };

    [Test]
    public void Unity_plugin_Delegation_dll_exports_Epic_A_types_used_by_Runtime_hosts()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null, "repo root with unity/ProjectAegis");

        var dll = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Plugins",
            "ProjectAegis",
            "ProjectAegis.Delegation.dll");

        Assert.That(File.Exists(dll), Is.True, $"Missing plugin DLL — run tools/copy-delegation-assemblies.ps1. Path: {dll}");

        var exported = ReadPublicTypeFullNames(dll);
        var missing = RequiredTypeFullNames.Where(t => !exported.Contains(t)).ToArray();

        Assert.That(
            missing,
            Is.Empty,
            "Unity plugin ProjectAegis.Delegation.dll is stale (missing Epic A types). " +
            "Run from repo root: ./tools/copy-delegation-assemblies.ps1\n" +
            "Missing:\n  - " + string.Join("\n  - ", missing));
    }

    private static HashSet<string> ReadPublicTypeFullNames(string dllPath)
    {
        using var stream = File.OpenRead(dllPath);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            // Skip module type (<Module>)
            if (type.Name.IsNil)
            {
                continue;
            }

            var name = reader.GetString(type.Name);
            if (name is "<Module>" or null)
            {
                continue;
            }

            // Nested types use declaring type; hosts use top-level public types only.
            if (!type.GetDeclaringType().IsNil)
            {
                continue;
            }

            var ns = type.Namespace.IsNil ? string.Empty : reader.GetString(type.Namespace);
            var full = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            names.Add(full);
        }

        return names;
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var marker = Path.Combine(dir.FullName, "unity", "ProjectAegis", "Assets");
            if (Directory.Exists(marker))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
