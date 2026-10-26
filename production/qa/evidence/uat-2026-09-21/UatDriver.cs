using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class UatDriver
{
    const string ActionPath = @"c:\Users\dgorn\cmano-clone\cmano-clone\production\qa\evidence\uat-2026-09-21\uat-action.txt";

    public static string Main()
    {
        var action = File.Exists(ActionPath) ? File.ReadAllText(ActionPath).Trim() : "snapshot";
        var sb = new StringBuilder();
        sb.Append("action=").AppendLine(action);
        sb.Append("playing=").Append(EditorApplication.isPlaying)
            .Append(" paused=").Append(EditorApplication.isPaused)
            .AppendLine();
        if (action == "open-game-view")
        {
            var gv = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var win = EditorWindow.GetWindow(gv);
            win.Show();
            win.Focus();
            sb.Append("gameView=").AppendLine(win.position.ToString());
            return sb.ToString();
        }

        if (action.StartsWith("click:", StringComparison.Ordinal))
        {
            sb.AppendLine(ClickNamed(action.Substring(6)));
        }
        else if (action.StartsWith("select-unit:", StringComparison.Ordinal))
        {
            sb.AppendLine(SelectUnit(action.Substring("select-unit:".Length)));
        }
        else if (action.StartsWith("select-contact:", StringComparison.Ordinal))
        {
            sb.AppendLine(SelectContact(action.Substring("select-contact:".Length)));
        }
        else if (action.StartsWith("oob-index:", StringComparison.Ordinal))
        {
            sb.AppendLine(SelectOobContaining(action.Substring("oob-index:".Length)));
        }

        sb.AppendLine(Dump());
        return sb.ToString();
    }

    static string SelectUnit(string id)
    {
        var host = UnityEngine.Object.FindAnyObjectByType<ProjectAegis.Unity.Runtime.DelegationBridgeHost>();
        if (host == null) return "no-bridge";
        host.SelectUnit(id);
        return "SelectUnit " + id;
    }

    static string SelectContact(string id)
    {
        var host = UnityEngine.Object.FindAnyObjectByType<ProjectAegis.Unity.Runtime.DelegationBridgeHost>();
        if (host == null) return "no-bridge";
        host.SelectContact(id);
        return "SelectContact " + id;
    }

    static string SelectOobContaining(string token)
    {
        foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var list = doc.rootVisualElement?.Q<ListView>("oob-list");
            if (list == null) continue;
            var n = list.itemsSource == null ? 0 : list.itemsSource.Count;
            for (var i = 0; i < n; i++)
            {
                var item = list.itemsSource[i];
                var text = item == null ? "" : item.ToString();
                if (text.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    var prop = item?.GetType().GetProperty("UnitId");
                    text = prop == null ? text : Convert.ToString(prop.GetValue(item));
                }
                if (text != null && text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    list.selectedIndex = i;
                    return "oob-select index=" + i + " token=" + token + " text=" + text;
                }
            }
            return "oob-miss token=" + token + " count=" + n;
        }
        return "oob-list-missing";
    }

    static string ClickNamed(string name)
    {
        foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var el = doc.rootVisualElement?.Q(name);
            if (el == null) continue;
            if (el is Button button)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var clickable = button.clickable;
                var clickedField = clickable.GetType().GetField("clicked", flags);
                var del = clickedField == null ? null : clickedField.GetValue(clickable) as Delegate;
                if (del != null)
                {
                    del.DynamicInvoke();
                    return "clicked " + name + " via clicked-delegate text=" + button.text + " enabled=" + button.enabledSelf;
                }
                var methods = clickable.GetType().GetMethods(flags);
                var names = new StringBuilder();
                foreach (var method in methods)
                {
                    if (method.Name.IndexOf("Click", StringComparison.OrdinalIgnoreCase) >= 0
                        || method.Name.IndexOf("Invoke", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        names.Append(method.Name).Append('(').Append(method.GetParameters().Length).Append(") ");
                    }
                }
                return "no-clicked-delegate " + name + " methods=" + names;
            }
            return "found-not-button " + name + " type=" + el.GetType().Name;
        }
        return "missing " + name;
    }

    static string Dump()
    {
        var sb = new StringBuilder();
        var host = UnityEngine.Object.FindAnyObjectByType<ProjectAegis.Unity.Runtime.DelegationBridgeHost>();
        if (host == null)
        {
            sb.AppendLine("bridge=missing");
        }
        else
        {
            sb.Append("policy=").Append(host.ScenarioPolicyId)
                .Append(" unit=").Append(host.SelectedUnitId)
                .Append(" contact=").Append(host.SelectedContactId)
                .Append(" compression=").Append(host.LiveCompressionLabel)
                .Append(" simPaused=").Append(host.Session != null && host.Session.IsSimPaused)
                .Append(" factor=").Append(host.Session == null ? -1 : host.Session.TimeAccelerationFactor)
                .AppendLine();
            var detail = host.LastUnitDetail;
            sb.Append("detailUnit=").Append(detail == null ? "" : detail.UnitId)
                .Append(" doctrine=").Append(detail == null ? "" : detail.DoctrineLabel)
                .AppendLine();
            var doctrine = host.LastDoctrineInheritance;
            sb.Append("inheritanceRoe=").Append(doctrine == null ? "" : doctrine.EffectiveRoeLabel)
                .Append(" simTick=").Append(host.CurrentSimTick)
                .AppendLine();
            var policy = host.Bridge == null || host.Bridge.Orchestrator == null
                ? null
                : host.Bridge.Orchestrator.ScenarioPolicy;
            sb.Append("orchestratorPolicy=")
                .Append(policy == null ? "null" : policy.FriendlyDefault.Roe.ToString())
                .Append("/")
                .Append(policy == null ? "null" : policy.OpposingDefault.Roe.ToString())
                .Append(" loadedFrom=").Append(ProjectAegis.Data.Scenario.Policy.ScenarioPolicyJsonIndex.LoadedFromDirectory)
                .AppendLine();
            var sim = UnityEngine.Object.FindAnyObjectByType<ProjectAegis.Unity.Runtime.SimplePlayModeSimHost>();
            sb.Append("simHost=").Append(sim == null ? "missing" : sim.enabled.ToString())
                .Append(" active=").Append(sim != null && sim.gameObject.activeInHierarchy)
                .Append(" timeScale=").Append(Time.timeScale.ToString("0.##"))
                .Append(" frame=").Append(Time.frameCount)
                .AppendLine();
        }

        string[] names = {
            "sim-time-label", "compression-label", "pause-resume-button",
            "phase-label", "attack-options-line", "unit-id-line", "contact-line",
            "attack-fire-single", "attack-hold-fire", "mission-list", "oob-list",
            "authority-roe-roe", "roe-label", "doctrine-line"
        };
        foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;
            foreach (var name in names)
            {
                var matches = root.Query<VisualElement>(name).ToList();
                if (matches.Count == 0) continue;
                var n = 0;
                foreach (var el in matches)
                {
                    n++;
                    var text = el is TextElement te ? te.text : "";
                    var bound = el.worldBound;
                    sb.Append(name).Append('#').Append(n)
                        .Append(" go=").Append(doc.gameObject.name)
                        .Append(" disp=").Append(el.resolvedStyle.display.ToString())
                        .Append(" font=").Append(el.resolvedStyle.fontSize.ToString("0.#"))
                        .Append(" h=").Append(bound.height.ToString("0.#"))
                        .Append(" w=").Append(bound.width.ToString("0.#"))
                        .Append(" text=").Append(text == null ? "" : text.Replace("\n", " | "))
                        .AppendLine();
                }
            }
            var mission = root.Q<ListView>("mission-list");
            if (mission != null)
            {
                var count = mission.itemsSource == null ? 0 : mission.itemsSource.Count;
                sb.Append("mission-list-count=").AppendLine(count.ToString());
            }
            var oob = root.Q<ListView>("oob-list");
            if (oob != null)
            {
                var count = oob.itemsSource == null ? 0 : oob.itemsSource.Count;
                sb.Append("oob-count=").Append(count).Append(" selected=").AppendLine(oob.selectedIndex.ToString());
            }
        }

        var top = GameObject.Find("C2TopBar");
        sb.Append("topBarActive=").AppendLine((top != null && top.activeInHierarchy).ToString());
        var map = GameObject.Find("MapPlaceholder");
        sb.Append("mapActive=").AppendLine((map != null && map.activeInHierarchy).ToString());
        foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;
            root.Query<TextElement>().ForEach(te =>
            {
                var text = te.text;
                if (string.IsNullOrEmpty(text)) return;
                if (text.IndexOf("ROE", StringComparison.OrdinalIgnoreCase) < 0 &&
                    text.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) < 0)
                    return;
                sb.Append("roe-scan name=").Append(te.name)
                    .Append(" text=").AppendLine(text.Replace("\n", " | "));
            });
        }
        return sb.ToString();
    }
}
