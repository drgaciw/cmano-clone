// UAT harness for MCP script-execute (include: ["UatHarness.cs"]). Play Mode only; never saves scenes.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UIE = UnityEngine.UIElements;

public static class UatHarness
{
    // Host type -> (uxml, uss). uss may be missing on disk; then styles stay null.
    public static readonly Dictionary<string, string> Uxml = new Dictionary<string, string>
    {
        ["UnitOrderToolbarHost"] = "UnitOrderToolbar/UnitOrderToolbarPanel",
        ["SensorToShooterPanelHost"] = "SensorToShooter/SensorToShooterPanel",
        ["MissionPackagePanelHost"] = "MissionPackage/MissionPackagePanel",
        ["AgentRosterPanelHost"] = "AgentRoster/AgentRosterPanel",
        ["AirOpsPanelHost"] = "AirOps/AirOpsPanel",
        ["ScenarioLibraryPanelHost"] = "ScenarioLibrary/ScenarioLibraryPanel",
        ["LiveEditPanelHost"] = "LiveEdit/LiveEditPanel",
        ["BoatOpsPanelHost"] = "BoatOps/BoatOpsPanel",
        ["MagazineLoadoutPanelHost"] = "MagazineLoadout/MagazineLoadoutPanel",
        ["DeckHangarPanelHost"] = "DeckHangar/DeckHangarPanel",
        ["C2MenuPanelHost"] = "C2Menu/C2MenuPanel",
        ["PendingApprovalPanelHost"] = "PendingApproval/PendingApprovalPanel",
        ["EngageExplainPanelHost"] = "EngageExplain/EngageExplainPanel",
        ["AuthorityRoePanelHost"] = "AuthorityRoe/AuthorityRoePanel",
        ["AxisControlPanelHost"] = "AxisControl/AxisControlPanel",
        ["MapScaleHudPanelHost"] = "MapScaleHud/MapScaleHudPanel",
        ["GroundOpsPanelHost"] = "GroundOps/GroundOpsPanel",
        ["BattleGraphicPanelHost"] = "BattleGraphic/BattleGraphicPanel",
        ["MissionListPanelHost"] = "MissionList/MissionListPanel",
        ["OobTreePanelHost"] = "OobTree/OobTreePanel",
        ["SensorC2PanelHost"] = "SensorC2/SensorC2Panel",
        ["JevAttentionPanelHost"] = "Jev/JevAttentionPanel",
        ["PlatformEditorShellHost"] = "PlatformEditor/PlatformEditorShell",
        ["ScenarioEditorShellHost"] = "ScenarioEditor/ScenarioEditorShell",
    };

    public static Type? FindType(string name) =>
        AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .SelectMany(t => t)
            .FirstOrDefault(t => t.Name == name && typeof(MonoBehaviour).IsAssignableFrom(t));

    static FieldInfo? Field(Type t, string name)
    {
        for (var c = t; c != null; c = c.BaseType)
        {
            var f = c.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null) return f;
        }
        return null;
    }

    static void Set(object o, string field, object? value)
    {
        var f = Field(o.GetType(), field);
        if (f != null && (value == null || f.FieldType.IsInstanceOfType(value))) f.SetValue(o, value);
    }

#if UNITY_EDITOR
    static T? Load<T>(string path) where T : UnityEngine.Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#endif

    /// <summary>Spawns every host in <see cref="Uxml"/> not already present. Returns a per-host status report.</summary>
    public static string SpawnMissing()
    {
        if (!Application.isPlaying) return "ERROR: not in Play Mode";
        var sb = new StringBuilder();
        var bridgeType = FindType("DelegationBridgeHost");
        var bridge = bridgeType != null ? UnityEngine.Object.FindFirstObjectByType(bridgeType) : null;
        var anyDoc = UnityEngine.Object.FindObjectsByType<UIE.UIDocument>(FindObjectsSortMode.None).FirstOrDefault(d => d.panelSettings != null);
        foreach (var kv in Uxml)
        {
            var t = FindType(kv.Key);
            if (t == null) { sb.AppendLine($"{kv.Key}: TYPE NOT FOUND"); continue; }
            if (UnityEngine.Object.FindFirstObjectByType(t) != null) { sb.AppendLine($"{kv.Key}: already present"); continue; }
            var go = new GameObject("UAT_" + kv.Key);
            go.SetActive(false);
            var doc = go.AddComponent<UIE.UIDocument>();
            if (anyDoc != null) { doc.panelSettings = anyDoc.panelSettings; }
            var host = go.AddComponent(t);
            if (bridge != null) Set(host, "bridgeHost", bridge);
            var note = "";
#if UNITY_EDITOR
            var vta = Load<UIE.VisualTreeAsset>($"Assets/UI/{kv.Value}.uxml");
            var uss = Load<UIE.StyleSheet>($"Assets/UI/{kv.Value}.uss");
            if (vta == null) note += " [uxml MISSING]";
            if (uss == null) note += " [uss missing]";
            Set(host, "panelAsset", vta);
            Set(host, "panelStyles", uss);
#endif
            try { go.SetActive(true); sb.AppendLine($"{kv.Key}: spawned{note}"); }
            catch (Exception e) { sb.AppendLine($"{kv.Key}: EXCEPTION on enable {e.GetType().Name}: {e.Message}{note}"); }
        }
        return sb.ToString();
    }

    static readonly Type[] ControlTypes =
    {
        typeof(UIE.Button), typeof(UIE.Toggle), typeof(UIE.DropdownField), typeof(UIE.TextField),
        typeof(UIE.Slider), typeof(UIE.SliderInt), typeof(UIE.IntegerField), typeof(UIE.FloatField),
        typeof(UIE.EnumField), typeof(UIE.RadioButton), typeof(UIE.Foldout), typeof(UIE.ListView), typeof(UIE.TreeView),
    };

    public static IEnumerable<UIE.UIDocument> Docs() =>
        UnityEngine.Object.FindObjectsByType<UIE.UIDocument>(FindObjectsSortMode.None).OrderBy(d => d.name);

    public static UIE.VisualElement? Root(string doc) => Docs().FirstOrDefault(d => d.name == doc)?.rootVisualElement;

    static string Label(UIE.VisualElement e) => e switch
    {
        UIE.Button b => b.text,
        UIE.Foldout f => f.text,
        UIE.BaseField<bool> t => t.label + (t is UIE.Toggle tg ? $"={tg.value}" : ""),
        UIE.DropdownField d => d.label + "=" + d.value + " {" + string.Join("/", d.choices ?? new List<string>()) + "}",
        UIE.TextField tf => tf.label + "=" + tf.value,
        _ => e.GetType().Name,
    };

    static bool Shown(UIE.VisualElement e)
    {
        for (var c = e; c != null; c = c.parent)
            if (c.resolvedStyle.display == UIE.DisplayStyle.None || c.resolvedStyle.visibility == UIE.Visibility.Hidden) return false;
        return e.worldBound.width > 0 && e.worldBound.height > 0;
    }

    /// <summary>TSV: doc, type, name, label, enabled, shown. Foldouts' inner Toggle is skipped.</summary>
    public static string Inventory(string? docFilter = null)
    {
        var sb = new StringBuilder("doc\ttype\tname\tlabel\tenabled\tshown\n");
        foreach (var d in Docs())
        {
            if (docFilter != null && d.name != docFilter) continue;
            var root = d.rootVisualElement;
            if (root == null) { sb.AppendLine($"{d.name}\t(null root)"); continue; }
            var all = UIE.UQueryExtensions.Query<UIE.VisualElement>(root).ToList();
            var controls = all.Where(e => ControlTypes.Any(t => t.IsInstanceOfType(e)))
                .Where(e => !(e is UIE.Toggle && e.parent?.parent is UIE.Foldout)).ToList();
            if (controls.Count == 0) sb.AppendLine($"{d.name}\t(no controls; elements={all.Count})");
            foreach (var c in controls)
                sb.AppendLine($"{d.name}\t{c.GetType().Name}\t{c.name}\t{Label(c).Replace('\n', ' ').Replace('\t', ' ')}\t{c.enabledInHierarchy}\t{Shown(c)}");
        }
        return sb.ToString();
    }

    public static UIE.VisualElement? Find(string doc, string name)
    {
        var r = Root(doc);
        return r == null ? null : UIE.UQueryExtensions.Q(r, name);
    }

    /// <summary>Press a button (or toggle a Toggle) the way keyboard/gamepad submit does.</summary>
    public static string Press(string doc, string name)
    {
        var e = Find(doc, name);
        if (e == null) return $"NOT FOUND {doc}/{name}";
        if (!e.enabledInHierarchy) return $"DISABLED {doc}/{name}";
        if (e is UIE.Toggle t) { t.value = !t.value; return $"toggled {name}={t.value}"; }
        using (var ev = UIE.NavigationSubmitEvent.GetPooled()) { ev.target = e; e.SendEvent(ev); }
        return $"pressed {doc}/{name}";
    }

    public static string SetValue(string doc, string name, string value)
    {
        var e = Find(doc, name);
        switch (e)
        {
            case null: return $"NOT FOUND {doc}/{name}";
            case UIE.DropdownField d: d.value = value; return $"{name}={d.value}";
            case UIE.TextField tf: tf.value = value; return $"{name}={tf.value}";
            case UIE.Toggle tg: tg.value = value == "true"; return $"{name}={tg.value}";
            case UIE.Slider s: s.value = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); return $"{name}={s.value}";
            case UIE.SliderInt si: si.value = int.Parse(value); return $"{name}={si.value}";
            default: return $"UNSUPPORTED {e.GetType().Name}";
        }
    }

    /// <summary>Visible label text in a document (for before/after diffs).</summary>
    public static string Text(string doc, int max = 40)
    {
        var r = Root(doc);
        if (r == null) return "(no doc)";
        return string.Join(" | ", UIE.UQueryExtensions.Query<UIE.Label>(r).ToList()
            .Where(l => !string.IsNullOrWhiteSpace(l.text) && Shown(l)).Select(l => l.text.Replace('\n', ' ')).Take(max));
    }
}
