using System.Text.Json;
using System.Text.Json.Nodes;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>
/// Acceptance criterion 3: when inject comms[] or multi-domain pairing is stripped,
/// harness mid-run state / concurrent launches disappear — positive gates would fail.
/// Restores the default scenario catalog after each mutation.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessLadderNegativeGateTests
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    [Test]
    public void Stripped_t4_inject_comms_emits_no_mid_run_comms_state_change()
    {
        using var scope = MutatedCatalogScope.Create(srcDir =>
        {
            var path = Path.Combine(srcDir, "gauntlet-t4-random-inject.policy.json");
            var node = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            node.Remove("comms");
            if (node["mission"] is JsonObject mission)
            {
                mission.Remove("triggers");
            }

            File.WriteAllText(path, node.ToJsonString(JsonOpts));
        });

        var result = BalticReplayHarness.Run(42, "gauntlet-t4-random-inject", ticks: 12, mvpEngagement: true);
        var midRunComms = result.DecisionLog.CommsStateChanges
            .Where(c => c.SimTick >= 1ul
                        && c.NewState is CommsState.Degraded or CommsState.Denied)
            .ToList();

        Assert.That(
            midRunComms,
            Is.Empty,
            "without policy comms[], harness must not invent mid-run CommsStateChange");
        Assert.That(
            result.Fingerprint,
            Does.Not.Contain("seeded_inject_jamming"),
            "inject reason token must not appear when comms[] stripped");
    }

    [Test]
    public void Collapsed_detection_pairing_prevents_distinct_air_and_sub_true_launched()
    {
        using var scope = MutatedCatalogScope.Create(srcDir =>
        {
            var path = Path.Combine(srcDir, "gauntlet-t3-emcon-phases.policy.json");
            var node = JsonNode.Parse(File.ReadAllText(path))!.AsObject();

            // Drop distinct reds so multi-domain pairing cannot assign different victims
            if (node["gauntlet"]?["units"] is JsonArray units)
            {
                for (var i = units.Count - 1; i >= 0; i--)
                {
                    var id = units[i]?["unitId"]?.GetValue<string>();
                    if (id is "mrk-buyan-pr-21630-buyan-2007"
                        or "mpk-steregushchiy-pr-20380-2018")
                    {
                        units.RemoveAt(i);
                    }
                }
            }

            // Force air + sub + surface onto the single remaining Sovremenny target
            // (SwarmSalvoDeconfliction: one shooter per target → concurrent multi-domain fails)
            if (node["detection"] is JsonArray det)
            {
                foreach (var t in det)
                {
                    if (t is not JsonObject)
                    {
                        continue;
                    }

                    var obs = t["observerId"]?.GetValue<string>();
                    if (obs is "a-19-gotland-2022" or "jas-39c-gripen-2005" or "k-31-visby-2009")
                    {
                        t["targetId"] = "em-sovremenny-i-pr-956-sarych";
                    }
                }
            }

            File.WriteAllText(path, node.ToJsonString(JsonOpts));
        });

        var result = BalticReplayHarness.Run(42, "gauntlet-t3-emcon-phases", ticks: 10, mvpEngagement: true);
        var tokens = result.Fingerprint.Split(
            ['\n', '\r', ' '],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var airLaunched = false;
        var subLaunched = false;
        string? airVictim = null;
        string? subVictim = null;
        for (var i = 0; i < tokens.Length; i++)
        {
            var e = tokens[i];
            if (!e.StartsWith("Engagement|", StringComparison.Ordinal)
                || !e.Contains("|True|Launched", StringComparison.Ordinal))
            {
                continue;
            }

            var shooter = e.Split('|')[4];
            var victim = FindNextOutcomeVictim(tokens, i + 1);
            if (shooter == "jas-39c-gripen-2005")
            {
                airLaunched = true;
                airVictim = victim;
            }
            else if (shooter == "a-19-gotland-2022")
            {
                subLaunched = true;
                subVictim = victim;
            }
        }

        // Multi-domain concurrent bar fails: not both domains launch at distinct reds
        var multiDomainOk = airLaunched && subLaunched
                            && airVictim != null
                            && subVictim != null
                            && !string.Equals(airVictim, subVictim, StringComparison.Ordinal);
        Assert.That(
            multiDomainOk,
            Is.False,
            "collapsed pairing (single red for air+sub) must not satisfy multi-domain bar");
    }

    private static string? FindNextOutcomeVictim(IReadOnlyList<string> tokens, int from)
    {
        for (var j = from; j < tokens.Count; j++)
        {
            if (tokens[j].StartsWith("EngagementOutcome|", StringComparison.Ordinal))
            {
                var parts = tokens[j].Split('|');
                return parts.Length >= 6 ? parts[5] : null;
            }

            if (tokens[j].StartsWith("Engagement|", StringComparison.Ordinal))
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Copies default scenarios, applies mutation, reloads catalog; restores default on dispose.
    /// </summary>
    private sealed class MutatedCatalogScope : IDisposable
    {
        private readonly string _defaultDir;
        private readonly string _tempDir;

        private MutatedCatalogScope(string defaultDir, string tempDir)
        {
            _defaultDir = defaultDir;
            _tempDir = tempDir;
        }

        public static MutatedCatalogScope Create(Action<string> mutateCopiedDir)
        {
            ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
            var defaultDir = ProjectAegis.Data.Scenario.Policy.ScenarioPolicyJsonIndex.LoadedFromDirectory
                             ?? throw new InvalidOperationException("default scenarios dir not loaded");
            var tempDir = Path.Combine(Path.GetTempPath(), "gauntlet-neg-" + Guid.NewGuid().ToString("N"));
            CopyDirectory(defaultDir, tempDir);
            mutateCopiedDir(tempDir);
            ScenarioPolicyRepository.LoadFromDirectory(tempDir);
            return new MutatedCatalogScope(defaultDir, tempDir);
        }

        public void Dispose()
        {
            try
            {
                ScenarioPolicyRepository.LoadFromDirectory(_defaultDir);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(_tempDir))
                    {
                        Directory.Delete(_tempDir, recursive: true);
                    }
                }
                catch
                {
                    // best-effort cleanup
                }
            }
        }

        private static void CopyDirectory(string src, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.EnumerateFiles(src, "*.policy.json"))
            {
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
            }
        }
    }
}
