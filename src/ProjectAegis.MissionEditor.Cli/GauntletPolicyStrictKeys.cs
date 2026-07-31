namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;

/// <summary>
/// Strict unknown-key validation for the qa-gauntlet policy block.
/// Root-cause guard for BUG-gauntlet-emcon-dimension-not-exercised: unknown keys under
/// <c>gauntlet.*</c> were silently dropped by System.Text.Json, letting an entire ladder
/// dimension go inert unnoticed. Whitelist = union of ScenarioGauntletJsonDto properties
/// and keys consumed by GauntletOracleEvaluator (expect/expectCi) — see spec 2026-07-28
/// (docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md).
/// </summary>
public static class GauntletPolicyStrictKeys
{
    private static readonly HashSet<string> GauntletKeys = new(StringComparer.Ordinal)
    {
        "intent", "oracle", "catalogRefs", "units", "expect", "expectCi", "runId", "tier",
        // QA metadata landed on main 2026-07-27/28: expect-regen provenance, the
        // variability plan's dimension claims, and forge candidate metadata.
        "expectProvenance", "expectCiProvenance", "dimensionsClaimed", "forge",
    };

    private static readonly HashSet<string> ExpectKeys = new(StringComparer.Ordinal)
    {
        "side", "minKills", "maxMissilesFired", "minDenials", "maxDenials",
        "minScore", "maxScore", "requireNonEmptyFingerprint",
        "requireFingerprintSubstrings", "requireTrueLaunchedShooters",
    };

    private static readonly HashSet<string> UnitKeys = new(StringComparer.Ordinal)
    { "unitId", "platformId", "domain", "side" };

    // Grandfathered until the 2026-07-27 variability plan retrofits the three shipped
    // EMCON policies; then move "emcon" from warn to error and add "dimensionsClaimed"
    // to GauntletKeys (that plan owns both changes).
    private static readonly HashSet<string> LegacyWarnKeys = new(StringComparer.Ordinal)
    { "emcon" };

    public static GauntletStrictKeyReport Check(string policyJson)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(policyJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("gauntlet", out var gauntlet)
                || gauntlet.ValueKind != JsonValueKind.Object)
            {
                return new GauntletStrictKeyReport(errors, warnings);
            }

            foreach (var prop in gauntlet.EnumerateObject())
            {
                if (GauntletKeys.Contains(prop.Name))
                {
                    continue;
                }

                if (LegacyWarnKeys.Contains(prop.Name))
                {
                    warnings.Add(
                        $"gauntlet.{prop.Name}: legacy stand-in key ignored by the engine — real EMCON is the top-level \"emcon\" block (BUG-gauntlet-emcon-dimension-not-exercised)");
                    continue;
                }

                errors.Add(
                    $"gauntlet.{prop.Name}: unknown key (silently ignored by the engine). Allowed: {string.Join(", ", GauntletKeys.OrderBy(k => k, StringComparer.Ordinal))}");
            }

            CheckObjectKeys(gauntlet, "expect", ExpectKeys, errors);
            CheckObjectKeys(gauntlet, "expectCi", ExpectKeys, errors);

            if (gauntlet.TryGetProperty("units", out var units) && units.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var unit in units.EnumerateArray())
                {
                    if (unit.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in unit.EnumerateObject())
                        {
                            if (!UnitKeys.Contains(prop.Name))
                            {
                                errors.Add(
                                    $"gauntlet.units[{i}].{prop.Name}: unknown key. Allowed: {string.Join(", ", UnitKeys.OrderBy(k => k, StringComparer.Ordinal))}");
                            }
                        }
                    }

                    i++;
                }
            }
        }
        catch (JsonException)
        {
            // Invalid JSON is surfaced by the evaluator; strict keys stay silent.
        }

        return new GauntletStrictKeyReport(errors, warnings);
    }

    private static void CheckObjectKeys(
        JsonElement gauntlet, string blockName, HashSet<string> allowed, List<string> errors)
    {
        if (!gauntlet.TryGetProperty(blockName, out var block) || block.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var prop in block.EnumerateObject())
        {
            if (!allowed.Contains(prop.Name))
            {
                errors.Add(
                    $"gauntlet.{blockName}.{prop.Name}: unknown key. Allowed: {string.Join(", ", allowed.OrderBy(k => k, StringComparer.Ordinal))}");
            }
        }
    }
}

/// <summary>Strict-key findings: errors fail oracle eval; warnings are reported only.</summary>
public sealed record GauntletStrictKeyReport(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
