using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// Guards config-only edits to the committed DelegationSmoke scene (UAT D15 subset f787b3a2, UAT D37 716b3dfd).
/// MonoBehaviours are resolved by script GUID read from the .cs.meta, never hardcoded.
/// </summary>
[TestFixture]
public sealed class DelegationSmokeSceneGuardTests
{
    private const int ToolbarSortingOrder = 12; // clears the map at 0, stays below side panels (15+)

    private static string ReadScene() =>
        UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "Scenes", "DelegationSmoke.unity");

    private static string ScriptGuid(string scriptName)
    {
        var meta = UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "Scripts", "Runtime", scriptName + ".cs.meta");
        var m = Regex.Match(meta, @"^guid:\s*([0-9a-f]{32})\s*$", RegexOptions.Multiline);
        Assert.That(m.Success, Is.True, $"guid missing in {scriptName}.cs.meta");
        return m.Groups[1].Value;
    }

    private static List<string> Documents(string scene) =>
        Regex.Split(scene, @"^--- !u!", RegexOptions.Multiline).Skip(1).ToList();

    private static List<string> MonoBehavioursWithScript(List<string> docs, string guid) =>
        docs.Where(d => Regex.IsMatch(d, @"^\d+ &\d+\s*\nMonoBehaviour:") &&
                        Regex.IsMatch(d, @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*" + guid + @",\s*type:\s*3\}"))
            .ToList();

    private static string Field(string doc, string name)
    {
        var m = Regex.Match(doc, @"^  " + Regex.Escape(name) + @":\s*(.+?)\s*$", RegexOptions.Multiline);
        Assert.That(m.Success, Is.True, $"{name} missing in scene document");
        return m.Groups[1].Value;
    }

    [Test]
    public void Scene_contains_unit_order_toolbar_above_map_layer()
    {
        var docs = Documents(ReadScene());
        var hosts = MonoBehavioursWithScript(docs, ScriptGuid("UnitOrderToolbarHost"));
        Assert.That(hosts, Has.Count.EqualTo(1), "scene must contain exactly one UnitOrderToolbarHost");

        var go = Regex.Match(Field(hosts[0], "m_GameObject"), @"fileID:\s*(\d+)").Groups[1].Value;
        var siblings = docs.Where(d =>
            Regex.IsMatch(d, @"^\d+ &\d+\s*\nMonoBehaviour:") &&
            Regex.IsMatch(d, @"^  m_GameObject:\s*\{fileID:\s*" + go + @"\}", RegexOptions.Multiline) &&
            d.Contains("UnityEngine.UIElements.UIDocument")).ToList();
        Assert.That(siblings, Has.Count.EqualTo(1), "toolbar GameObject must carry one UIDocument");

        Assert.That(int.Parse(Field(siblings[0], "m_SortingOrder")), Is.EqualTo(ToolbarSortingOrder),
            "toolbar UIDocument sorting order must be 12 (0 ties with the map and hides it)");
    }

    [Test]
    public void Scene_shows_doctrine_inheritance_panel()
    {
        var hosts = MonoBehavioursWithScript(Documents(ReadScene()), ScriptGuid("DoctrineInheritancePanelHost"));
        Assert.That(hosts, Has.Count.EqualTo(1), "scene must contain exactly one DoctrineInheritancePanelHost");
        Assert.That(Field(hosts[0], "showPanel"), Is.EqualTo("1"));
    }
}
