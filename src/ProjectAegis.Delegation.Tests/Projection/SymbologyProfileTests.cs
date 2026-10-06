using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// S126 symbology subset (DRG-231 SYM-MIL-01, MIL-only; DRG-232 CIV pending).
/// Headless proof for canonical keys (W2-SYM-01), profile switch (SYM-03),
/// scoped naval types (W3-SYM-01), disclaimer (W2-SYM-03) and freeze (W3-SYM-03).
/// </summary>
[TestFixture]
public sealed class SymbologyProfileTests
{
    private static readonly IReadOnlyList<MapSymbolEntry> SamplePicture = MapPictureProjection.Project(
        [new OobTreeEntry("u1", IsAlive: true), new OobTreeEntry("u2", IsAlive: false)],
        [
            new ContactPictureEntry("c-1", "hostile-1", "u1", "Classified", 3, 3.0),
            new ContactPictureEntry("c-2", "hostile-2", "u1", "Detected", 3, 3.0),
        ],
        layoutSeed: 7);

    private static readonly IReadOnlyDictionary<string, string> ObservedKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["u1"] = SymbolKeyRegistry.NavalSurfaceCombatant,
            ["u2"] = SymbolKeyRegistry.NavalSubsurfaceSubmarine,
            ["c-1"] = SymbolKeyRegistry.NavalSurfaceUnknown,
        };

    [Test]
    public void Registry_keys_are_canonical_unique_and_ordinal_sorted()
    {
        var keys = SymbolKeyRegistry.All.Select(d => d.Key).ToArray();

        Assert.That(keys, Is.Unique);
        Assert.That(keys, Is.EqualTo(keys.OrderBy(k => k, StringComparer.Ordinal).ToArray()));
        Assert.That(keys.All(SymbolKeyRegistry.IsCanonicalKeyFormat), Is.True);
        Assert.That(SymbolKeyRegistry.SchemaVersion, Is.EqualTo("aegis-sym-keys/v1"));
    }

    [Test]
    public void Registry_pins_stable_key_strings_for_scoped_naval_types()
    {
        Assert.That(SymbolKeyRegistry.NavalSurfaceCombatant, Is.EqualTo("naval.surface.combatant"));
        Assert.That(SymbolKeyRegistry.NavalSubsurfaceSubmarine, Is.EqualTo("naval.subsurface.submarine"));
        Assert.That(SymbolKeyRegistry.NavalSurfaceUnknown, Is.EqualTo("naval.surface.unknown"));
        Assert.That(SymbolKeyRegistry.GenericUnknown, Is.EqualTo("generic.unknown"));
        Assert.That(SymbolKeyRegistry.ScopedNavalKeys, Has.Count.GreaterThanOrEqualTo(3));
        Assert.That(
            SymbolKeyRegistry.ScopedNavalKeys.All(k => SymbolKeyRegistry.TryGet(k, out var d) && d.Domain == "naval"),
            Is.True);
    }

    [Test]
    public void Registry_every_entry_records_source_and_fallback()
    {
        foreach (var def in SymbolKeyRegistry.All)
        {
            Assert.That(def.SourceReference, Is.Not.Empty, def.Key);
            Assert.That(def.FunctionId, Has.Length.EqualTo(6), def.Key);
            Assert.That(SymbolKeyRegistry.TryGet(def.FallbackKey, out _), Is.True, def.Key);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("naval.surface.carrier")]
    [TestCase("NAVAL.SURFACE.COMBATANT")]
    public void Unrecognised_or_unobserved_key_falls_back_to_generic_unknown(string? key)
    {
        Assert.That(SymbolKeyRegistry.ResolveObserved(key).Key, Is.EqualTo(SymbolKeyRegistry.GenericUnknown));
    }

    [Test]
    public void Military_profile_pins_editions_and_ntds_subset()
    {
        Assert.That(MilitarySymbology.App6Edition, Does.Contain("APP-6(C)"));
        Assert.That(MilitarySymbology.MilStd2525Edition, Does.Contain("MIL-STD-2525C"));
        Assert.That(MilitarySymbology.NtdsSubset, Does.Contain("surface").And.Contain("subsurface"));
        Assert.That(
            MilitarySymbology.SupportedAffiliations,
            Is.EqualTo(new[] { "Friendly", "Hostile", "Neutral", "Unknown" }));
    }

    [Test]
    public void Military_scoped_naval_types_resolve_to_15_char_sidc_per_affiliation()
    {
        var combatant = MilitarySymbology.Resolve("Hostile", SymbolKeyRegistry.NavalSurfaceCombatant);
        var submarine = MilitarySymbology.Resolve("Friendly", SymbolKeyRegistry.NavalSubsurfaceSubmarine);
        var unknownSurface = MilitarySymbology.Resolve("Neutral", SymbolKeyRegistry.NavalSurfaceUnknown);

        Assert.That(combatant.Sidc, Is.EqualTo("SHSPC----------"));
        Assert.That(submarine.Sidc, Is.EqualTo("SFUPS----------"));
        Assert.That(unknownSurface.Sidc, Is.EqualTo("SNSP-----------"));
        Assert.That(new[] { combatant, submarine, unknownSurface }.All(g => App6Sidc.IsValidSidc(g.Sidc)), Is.True);
        Assert.That(combatant.SymbolKey, Is.EqualTo(SymbolKeyRegistry.NavalSurfaceCombatant));
        Assert.That(submarine.UssFrameId, Is.EqualTo("map-sym-mil--friendly-subsurface"));
    }

    [Test]
    public void Military_affiliations_are_distinguishable_by_shape_not_colour()
    {
        foreach (var key in SymbolKeyRegistry.All.Select(d => d.Key))
        {
            var glyphs = MilitarySymbology.SupportedAffiliations
                .Select(a => MilitarySymbology.Resolve(a, key).UnicodeGlyph)
                .ToArray();
            var frames = MilitarySymbology.SupportedAffiliations
                .Select(a => MilitarySymbology.Resolve(a, key).UssFrameId)
                .ToArray();
            Assert.That(glyphs, Is.Unique, key);
            Assert.That(frames, Is.Unique, key);
        }
    }

    [TestCase("Suspect")]
    [TestCase("Pending")]
    [TestCase("Joker")]
    [TestCase("")]
    [TestCase(null)]
    public void Military_affiliation_outside_frozen_set_degrades_to_unknown(string? affiliation)
    {
        var glyph = MilitarySymbology.Resolve(affiliation, SymbolKeyRegistry.NavalSurfaceCombatant);

        Assert.That(glyph.Affiliation, Is.EqualTo("Unknown"));
        Assert.That(glyph.Sidc[1], Is.EqualTo('U'));
    }

    [Test]
    public void Military_unobserved_type_never_invents_class_or_domain()
    {
        var glyph = MilitarySymbology.Resolve("Hostile", observedSymbolKey: null);

        Assert.That(glyph.SymbolKey, Is.EqualTo(SymbolKeyRegistry.GenericUnknown));
        Assert.That(glyph.Sidc, Is.EqualTo("SHZP-----------"));
        Assert.That(glyph.IconText, Is.EqualTo("?"));
    }

    [Test]
    public void Military_destroyed_own_unit_uses_destroyed_status_and_frame()
    {
        var glyph = MilitarySymbology.Resolve("Friendly", SymbolKeyRegistry.NavalSurfaceCombatant, isDestroyed: true);

        Assert.That(glyph.Sidc, Is.EqualTo("SFSXC----------"));
        Assert.That(glyph.UssFrameId, Does.EndWith("--destroyed"));
    }

    [Test]
    public void Apply_legacy_profile_is_identity_with_map_picture_projection()
    {
        var legacy = SymbologyProjection.Apply(SamplePicture, SymbologyProfile.Legacy, ObservedKeys);

        Assert.That(legacy, Is.EqualTo(SamplePicture));
    }

    [Test]
    public void Apply_military_profile_restyles_only_presentation_fields()
    {
        var mil = SymbologyProjection.Apply(SamplePicture, SymbologyProfile.MilitaryTactical, ObservedKeys);

        Assert.That(mil, Has.Count.EqualTo(SamplePicture.Count));
        for (var i = 0; i < mil.Count; i++)
        {
            var before = SamplePicture[i];
            var after = mil[i];
            Assert.That(after.SymbolId, Is.EqualTo(before.SymbolId));
            Assert.That(after.Affiliation, Is.EqualTo(before.Affiliation));
            Assert.That(after.Label, Is.EqualTo(before.Label));
            Assert.That(after.NormalizedX, Is.EqualTo(before.NormalizedX));
            Assert.That(after.NormalizedY, Is.EqualTo(before.NormalizedY));
            Assert.That(after.IsDestroyed, Is.EqualTo(before.IsDestroyed));
            Assert.That(after.HasAuthoritativePose, Is.EqualTo(before.HasAuthoritativePose));
        }

        var bySymbol = mil.ToDictionary(s => s.SymbolId, StringComparer.Ordinal);
        Assert.That(bySymbol["u1"].App6Sidc, Is.EqualTo("SFSPC----------"));
        Assert.That(bySymbol["u2"].App6Sidc, Is.EqualTo("SFUXS----------"));
        Assert.That(bySymbol["c-1"].App6Sidc, Is.EqualTo("SHSP-----------"));
        Assert.That(bySymbol["c-2"].App6Sidc, Is.EqualTo("SHZP-----------"));
    }

    [Test]
    public void Profile_round_trip_does_not_mutate_input_picture()
    {
        var fingerprintBefore = Fingerprint(SamplePicture);

        var mil = SymbologyProjection.Apply(SamplePicture, SymbologyProfile.MilitaryTactical, ObservedKeys);
        var back = SymbologyProjection.Apply(mil, SymbologyProfile.Legacy, ObservedKeys);
        var milAgain = SymbologyProjection.Apply(back, SymbologyProfile.MilitaryTactical, ObservedKeys);

        Assert.That(Fingerprint(SamplePicture), Is.EqualTo(fingerprintBefore));
        Assert.That(back, Is.EqualTo(SamplePicture));
        Assert.That(milAgain, Is.EqualTo(mil));
    }

    [Test]
    public void Switch_toggles_between_legacy_and_military_and_defaults_to_legacy()
    {
        var sw = new SymbologyProfileSwitch();

        Assert.That(sw.Current, Is.EqualTo(SymbologyProfile.Legacy));
        Assert.That(sw.Toggle(), Is.EqualTo(SymbologyProfile.MilitaryTactical));
        Assert.That(sw.Toggle(), Is.EqualTo(SymbologyProfile.Legacy));
    }

    [Test]
    public void Switch_rejects_civilian_profile_while_scope_decision_pending()
    {
        var sw = new SymbologyProfileSwitch(SymbologyProfile.MilitaryTactical);

        var accepted = sw.TrySelect(SymbologyProfile.CivilianFriendly, out var reason);

        Assert.That(accepted, Is.False);
        Assert.That(reason, Does.Contain("DRG-232"));
        Assert.That(sw.Current, Is.EqualTo(SymbologyProfile.MilitaryTactical));
        Assert.That(SymbologyProfileCatalog.IsSelectable(SymbologyProfile.CivilianFriendly), Is.False);
        Assert.Throws<ArgumentException>(() =>
            SymbologyProjection.Apply(SamplePicture, SymbologyProfile.CivilianFriendly, ObservedKeys));
    }

    [TestCase(SymbologyProfile.Legacy)]
    [TestCase(SymbologyProfile.MilitaryTactical)]
    public void Legend_surfaces_not_certified_disclaimer_for_every_selectable_profile(SymbologyProfile profile)
    {
        var legend = SymbologyLegendProjection.Build(profile);

        Assert.That(legend.Disclaimer, Is.EqualTo(SymbologyLegendProjection.NotCertifiedDisclaimer));
        Assert.That(legend.Disclaimer, Does.Contain("subset").And.Contain("not certified"));
        Assert.That(legend.CertificationClaimed, Is.False);
        Assert.That(legend.AffiliationExpansionStatus, Does.Contain("HOLD").And.Contain("W2-SYM-05"));
        Assert.That(legend.CivilianProfileStatus, Does.Contain("pending").IgnoreCase.And.Contain("DRG-232"));
        Assert.That(legend.Rows, Is.Not.Empty);
    }

    [Test]
    public void Military_legend_names_editions_and_lists_every_key_by_affiliation()
    {
        var legend = SymbologyLegendProjection.Build(SymbologyProfile.MilitaryTactical);

        Assert.That(legend.ProfileDisplayName, Is.EqualTo("Military Tactical (subset)"));
        Assert.That(legend.StandardsEdition, Does.Contain(MilitarySymbology.App6Edition));
        Assert.That(legend.StandardsEdition, Does.Contain(MilitarySymbology.MilStd2525Edition));
        Assert.That(legend.NtdsSubset, Is.EqualTo(MilitarySymbology.NtdsSubset));
        Assert.That(legend.Deviations, Is.Not.Empty);
        Assert.That(
            legend.Rows,
            Has.Count.EqualTo(SymbolKeyRegistry.All.Count * MilitarySymbology.SupportedAffiliations.Count));
        Assert.That(legend.Rows.All(r => r.Label.Contains(r.Affiliation, StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void Legend_is_deterministic()
    {
        var a = SymbologyLegendProjection.Build(SymbologyProfile.MilitaryTactical);
        var b = SymbologyLegendProjection.Build(SymbologyProfile.MilitaryTactical);

        Assert.That(b.Rows, Is.EqualTo(a.Rows));
        Assert.That(b.Deviations, Is.EqualTo(a.Deviations));
    }

    [Test]
    public void Sim_replay_and_hash_sources_do_not_reference_symbology()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var guarded = new List<string>();
        guarded.AddRange(Directory.EnumerateFiles(Path.Combine(root!, "src", "ProjectAegis.Sim"), "*.cs", SearchOption.AllDirectories));
        guarded.AddRange(Directory.EnumerateFiles(Path.Combine(root!, "src", "ProjectAegis.Delegation", "Replay"), "*.cs", SearchOption.AllDirectories));
        guarded.AddRange(Directory.EnumerateFiles(Path.Combine(root!, "src", "ProjectAegis.Delegation", "Decision"), "*.cs", SearchOption.AllDirectories));
        guarded.AddRange(Directory.EnumerateFiles(Path.Combine(root!, "src", "ProjectAegis.Delegation", "Orchestration"), "*.cs", SearchOption.AllDirectories));
        guarded.Add(Path.Combine(root!, "src", "ProjectAegis.Delegation.UnityAdapter", "Baltic", "BalticReplayHarness.cs"));
        guarded.Add(Path.Combine(root!, "src", "ProjectAegis.Delegation.UnityAdapter", "Bridge", "DelegationBridge.cs"));

        var tokens = new[] { "SymbolKeyRegistry", "SymbologyProfile", "SymbologyProjection", "MilitarySymbology", "SymbologyLegend" };
        var offenders = guarded
            .Where(File.Exists)
            .Where(path =>
            {
                var text = File.ReadAllText(path);
                return tokens.Any(t => text.Contains(t, StringComparison.Ordinal));
            })
            .ToArray();

        Assert.That(guarded.Count, Is.GreaterThan(10));
        Assert.That(offenders, Is.Empty);
    }

    [Test]
    public void Sim_assembly_does_not_reference_delegation_presentation()
    {
        var simAssembly = typeof(ProjectAegis.Sim.Policy.EffectivePolicy).Assembly;

        Assert.That(
            simAssembly.GetReferencedAssemblies().Select(a => a.Name),
            Has.None.EqualTo(typeof(SymbolKeyRegistry).Assembly.GetName().Name));
    }

    private static string Fingerprint(IReadOnlyList<MapSymbolEntry> symbols) =>
        string.Join("\n", symbols.Select(s => s.ToString()));

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            var parent = Directory.GetParent(dir);
            if (parent is null)
            {
                return null;
            }

            dir = parent.FullName;
        }

        return null;
    }
}
