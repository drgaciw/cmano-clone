// Play Mode probe for the 5-scenario UAT matrix: runs the sim briefly, then reports
// console errors, the top bar, and Unit Detail for the friendly unit.
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class ScenarioProbe
{
    public static async Task<string> Run(int runMs = 3000)
    {
        var errs = new System.Collections.Generic.List<string>();
        Application.LogCallback cb = (m, s, t) =>
        {
            if (t == LogType.Error || t == LogType.Exception) lock (errs) errs.Add(m.Split('\n')[0]);
        };
        Application.logMessageReceived += cb;
        await Task.Delay(800);
        UatHarness.Press("AttentionToast", "attention-toast-ack");
        await Task.Delay(400);
        UatHarness.Press("C2TopBar", "pause-resume-button");
        await Task.Delay(runMs);
        UatHarness.Press("C2TopBar", "pause-resume-button");
        await Task.Delay(400);
        var oob = UatHarness.Rows("C2LeftDrawer", "oob-list", 4);
        var lv = UatHarness.Find("C2LeftDrawer", "oob-list") as UnityEngine.UIElements.ListView;
        var friendly = -1;
        if (lv?.itemsSource != null)
        {
            var rows = lv.itemsSource.Cast<object>().ToList();
            friendly = rows.FindIndex(r => !(r?.ToString() ?? "").Contains("hostile"));
        }
        if (friendly >= 0) UatHarness.SelectRow("C2LeftDrawer", "oob-list", friendly);
        await Task.Delay(500);
        Application.logMessageReceived -= cb;
        string e;
        lock (errs) e = errs.Count == 0 ? "0 errors" : errs.Count + " errors: " + string.Join(" || ", errs.Distinct().Take(3));
        return "TOP: " + UatHarness.Text("C2TopBar", 12) + "\n" + oob + "\nUNIT: " + UatHarness.Text("RightUnitDetail", 14) + "\n" + e;
    }
}
