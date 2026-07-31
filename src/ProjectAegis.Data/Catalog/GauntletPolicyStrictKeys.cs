namespace ProjectAegis.Data.Catalog;

using System.Reflection;
using System.Text.Json;
using ProjectAegis.Data.Scenario.Policy;

/// <summary>
/// Strict unknown-key validation for the qa-gauntlet policy block.
/// Root-cause guard for BUG-gauntlet-emcon-dimension-not-exercised: unknown keys under
/// <c>gauntlet.*</c> were silently dropped by System.Text.Json, letting an entire ladder
/// dimension go inert unnoticed.
/// Allowed keys are derived from <see cref="ScenarioGauntletJsonDto"/> (and expect/unit DTOs)
/// via System.Text.Json camelCase naming — not a hand-maintained string encyclopedia.
/// </summary>
public static class GauntletPolicyStrictKeys
{
    private static readonly HashSet<string> GauntletKeys = DeriveCamelCaseKeys(typeof(ScenarioGauntletJsonDto));

    private static readonly HashSet<string> ExpectKeys = DeriveCamelCaseKeys(typeof(ScenarioGauntletExpectJsonDto));

    private static readonly HashSet<string> UnitKeys = DeriveCamelCaseKeys(typeof(ScenarioGauntletUnitJsonDto));

    // Time-boxed grandfather only — remove when variability EMCON retrofit deletes gauntlet.emcon.
    private static readonly HashSet<string> LegacyWarnKeys = new(StringComparer.Ordinal) { "emcon" };

    /// <summary>CamelCase root keys derived from <see cref="ScenarioGauntletJsonDto"/>.</summary>
    public static IReadOnlyCollection<string> AllowedGauntletKeys => GauntletKeys;

    /// <summary>CamelCase expect keys derived from <see cref="ScenarioGauntletExpectJsonDto"/>.</summary>
    public static IReadOnlyCollection<string> AllowedExpectKeys => ExpectKeys;

    /// <summary>CamelCase unit keys derived from <see cref="ScenarioGauntletUnitJsonDto"/>.</summary>
    public static IReadOnlyCollection<string> AllowedUnitKeys => UnitKeys;

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

    private static HashSet<string> DeriveCamelCaseKeys(Type dtoType)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var prop in dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            keys.Add(JsonNamingPolicy.CamelCase.ConvertName(prop.Name));
        }

        return keys;
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
