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

    // script-execute snippets are not compiled with UNITY_EDITOR defined, but always run in the Editor.
    static T? Load<T>(string path) where T : UnityEngine.Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

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
            var vta = Load<UIE.VisualTreeAsset>($"Assets/UI/{kv.Value}.uxml");
            var uss = Load<UIE.StyleSheet>($"Assets/UI/{kv.Value}.uss");
            if (vta == null) note += " [uxml MISSING]";
            if (uss == null) note += " [uss missing]";
            Set(host, "panelAsset", vta);
            Set(host, "panelStyles", uss);
            var check = Field(t, "panelAsset")?.GetValue(host) as UnityEngine.Object;
            if (vta != null && check == null) note += " [panelAsset NOT SET]";
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

    /// <summary>Selects row <paramref name="index"/> of a ListView (how the UI selects units, contacts, scenarios...).</summary>
    public static string SelectRow(string doc, string listName, int index)
    {
        if (!(Find(doc, listName) is UIE.BaseVerticalCollectionView lv)) return $"NOT FOUND {doc}/{listName}";
        var n = lv.itemsSource?.Count ?? 0;
        if (index >= n) return $"{listName}: only {n} rows";
        lv.SetSelection(index);
        return $"{listName}: selected {index} of {n} ({lv.selectedItem})";
    }

    public static string Rows(string doc, string listName, int max = 10)
    {
        if (!(Find(doc, listName) is UIE.BaseVerticalCollectionView lv)) return $"NOT FOUND {doc}/{listName}";
        var src = lv.itemsSource;
        if (src == null) return $"{listName}: itemsSource null";
        return $"{listName}: {src.Count} rows: " + string.Join(" ; ", src.Cast<object>().Take(max).Select(o => Trunc(o?.ToString() ?? "null", 70)));
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

    static readonly List<string> Errors = new List<string>();
    static bool _hooked;

    static void Hook()
    {
        if (_hooked) return;
        _hooked = true;
        Application.logMessageReceived += (msg, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                lock (Errors) Errors.Add(type + ": " + msg.Split('\n')[0] + " @ " + (stack ?? "").Split('\n').FirstOrDefault(l => l.Contains("ProjectAegis"))?.Trim());
        };
    }

    static Dictionary<string, string> SnapAll() => Docs().ToDictionary(d => d.name + "#" + d.GetInstanceID(), d => Text(d.name, 400) + " ¦ " + ControlState(d));

    static string ControlState(UIE.UIDocument d)
    {
        var r = d.rootVisualElement;
        if (r == null) return "";
        return string.Join(",", UIE.UQueryExtensions.Query<UIE.VisualElement>(r).ToList()
            .Where(e => ControlTypes.Any(t => t.IsInstanceOfType(e)))
            .Select(e => (e.enabledInHierarchy ? "E" : "d") + (Shown(e) ? "S" : "h") + Label(e)));
    }

    static string FirstDiff(string a, string b)
    {
        var x = a.Split(new[] { " | ", " ¦ ", "," }, StringSplitOptions.None);
        var y = b.Split(new[] { " | ", " ¦ ", "," }, StringSplitOptions.None);
        for (var i = 0; i < Math.Max(x.Length, y.Length); i++)
        {
            var p = i < x.Length ? x[i] : "∅";
            var q = i < y.Length ? y[i] : "∅";
            if (p != q) return Trunc(p, 50) + " → " + Trunc(q, 50);
        }
        return "";
    }

    static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n) + "…";

    /// <summary>
    /// Presses every enabled + shown control (optionally only in docs whose name contains <paramref name="docFilter"/>,
    /// skipping names in <paramref name="skip"/>), waiting <paramref name="settleMs"/> after each, and records the effect.
    /// Returns TSV: doc, control, type, action, ownDocDiff, otherDocsChanged, newErrors.
    /// </summary>
    static string Sig(UIE.VisualElement e) => e.GetType().Name + "|" + e.name + "|" + (e.parent?.name ?? "") + "|" + Trunc(Label(e), 30);

    static List<UIE.VisualElement> Controls(UIE.VisualElement root) =>
        UIE.UQueryExtensions.Query<UIE.VisualElement>(root).ToList()
            .Where(e => ControlTypes.Any(t => t.IsInstanceOfType(e)))
            .Where(e => !(e is UIE.Toggle && e.parent?.parent is UIE.Foldout))
            .Where(e => !(e is UIE.ListView) && !(e is UIE.TreeView)).ToList();

    static bool IsCollapse(UIE.VisualElement e) => e.name.Contains("collapse") || Label(e).StartsWith("Collapse") || Label(e).StartsWith("Expand");

    /// <summary>
    /// Presses every enabled + shown control in docs whose name contains <paramref name="docFilter"/> (skipping names in
    /// <paramref name="skip"/>), waiting <paramref name="settleMs"/> after each. Long sibling lists (more than 3 controls of the
    /// same type under one named parent) are sampled: first 2 only. Collapse toggles run last. <paramref name="done"/> carries
    /// signatures between passes. Returns TSV: doc, control, type, action, ownDocDiff, otherDocsChanged, newErrors.
    /// </summary>
    public static async System.Threading.Tasks.Task<string> Sweep(string? docFilter = null, string skip = "", int settleMs = 250, HashSet<string>? done = null)
    {
        Hook();
        var skipSet = new HashSet<string>(skip.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
        done ??= new HashSet<string>(); // pass the same set to a second sweep to cover controls revealed by the first
        var sb = new StringBuilder();
        var targets = new List<(string doc, string sig, string type, string label)>();
        foreach (var d in Docs())
        {
            if (docFilter != null && !d.name.Contains(docFilter)) continue;
            var r = d.rootVisualElement;
            if (r == null) continue;
            var list = Controls(r);
            var groups = list.Where(e => !string.IsNullOrEmpty(e.parent?.name) && string.IsNullOrEmpty(e.name))
                .GroupBy(e => e.GetType().Name + "|" + e.parent!.name).Where(g => g.Count() > 3)
                .ToDictionary(g => g.Key, g => g.Count());
            var seenInGroup = new Dictionary<string, int>();
            foreach (var c in list.OrderBy(e => IsCollapse(e) ? 1 : 0))
            {
                if (skipSet.Contains(c.name)) continue;
                var gk = c.GetType().Name + "|" + (c.parent?.name ?? "");
                var label = Label(c);
                if (string.IsNullOrEmpty(c.name) && groups.TryGetValue(gk, out var gcount))
                {
                    seenInGroup.TryGetValue(gk, out var n);
                    seenInGroup[gk] = n + 1;
                    if (n >= 2) { if (n == 2) sb.AppendLine($"{d.name}	[{c.parent!.name} rows]	{c.GetType().Name}	SAMPLED 2 of {gcount}			"); continue; }
                    label = c.parent!.name + "[" + n + "]";
                }
                var sig = d.name + "::" + Sig(c);
                if (done.Contains(sig)) continue;
                done.Add(sig);
                targets.Add((d.name, Sig(c), c.GetType().Name, string.IsNullOrEmpty(c.name) ? Trunc(label, 40) : c.name));
            }
        }

        foreach (var (doc, sig, type, label) in targets)
        {
            var root = Root(doc);
            var el = root == null ? null : Controls(root).FirstOrDefault(e => Sig(e) == sig);
            if (el == null) { sb.AppendLine($"{doc}	{label}	{type}	SKIP(gone)			"); continue; }
            if (!el.enabledInHierarchy || !Shown(el)) { sb.AppendLine($"{doc}	{label}	{type}	SKIP(disabled={!el.enabledInHierarchy},hidden={!Shown(el)})			"); continue; }

            var before = SnapAll();
            int errBefore; lock (Errors) errBefore = Errors.Count;
            string action;
            try
            {
                switch (el)
                {
                    case UIE.DropdownField dd:
                        var choices = dd.choices ?? new List<string>();
                        var next = choices.Count > 1 ? choices[(Math.Max(0, choices.IndexOf(dd.value)) + 1) % choices.Count] : dd.value;
                        dd.value = next; action = "select " + Trunc(next ?? "", 30);
                        break;
                    case UIE.TextField tf:
                        tf.value = "UAT"; action = "type UAT";
                        break;
                    case UIE.Toggle tg:
                        tg.value = !tg.value; action = "toggle→" + tg.value;
                        break;
                    case UIE.Foldout fo:
                        fo.value = !fo.value; action = "fold→" + fo.value;
                        break;
                    case UIE.Slider sl:
                        sl.value = sl.lowValue + (sl.highValue - sl.lowValue) * 0.75f; action = "slide→" + sl.value.ToString("0.##");
                        break;
                    default:
                        using (var ev = UIE.NavigationSubmitEvent.GetPooled()) { ev.target = el; el.SendEvent(ev); }
                        action = "press";
                        break;
                }
            }
            catch (Exception e) { action = "THREW " + e.GetType().Name + ": " + Trunc(e.Message, 80); }

            await System.Threading.Tasks.Task.Delay(settleMs);
            var after = SnapAll();
            var own = before.Keys.Where(k => k.StartsWith(doc + "#")).Select(k => after.TryGetValue(k, out var v) ? FirstDiff(before[k], v) : "doc gone").FirstOrDefault(x => x != "") ?? "";
            var others = string.Join(",", before.Keys.Where(k => !k.StartsWith(doc + "#") && after.TryGetValue(k, out var v) && v != before[k]).Select(k => k.Split('#')[0]));
            string errs; lock (Errors) errs = string.Join(" || ", Errors.Skip(errBefore).Distinct().Take(3));
            sb.AppendLine($"{doc}	{label}	{type}	{action}	{own.Replace('\t', ' ')}	{others}	{errs.Replace('\t', ' ')}");
        }
        return sb.ToString();
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
