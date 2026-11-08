using System.Globalization;
using System.Reflection;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// DRG-411: ambiguity versus weapon tolerance stays unknown until a producer supplies
/// measurements, and test-supplied measurements compare without inventing doctrine.
/// </summary>
[TestFixture]
public sealed class WeaponContactAmbiguityProjectionTests
{
    private static readonly string[] ProductionSources =
    {
        "WeaponContactAmbiguity.cs",
        "WeaponContactAmbiguityProjection.cs",
        "WeaponContactAmbiguityFingerprint.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "UnityEngine",
        "MonoBehaviour",
        "ApplyOrder",
        "DecisionLog",
        "SimulationSession",
        "DelegationBridge",
    };

    [Test]
    public void Provenance_and_weapon_ids_project_unknown_pairs()
    {
        var contacts = new[]
        {
            Contact("c2", ContactProvenanceConfidence.High),
            Contact("c1", ContactProvenanceConfidence.Low),
            Contact("c2", ContactProvenanceConfidence.Unknown),
        };
        var weapons = new[] { "weapon-b", "weapon-a", "weapon-b", "" };

        var snapshot = WeaponContactAmbiguityProjection.Project(contacts, weapons);

        Assert.That(snapshot.Rows, Has.Count.EqualTo(4));
        Assert.That(snapshot.Rows.Select(row => (row.WeaponId, row.ContactId)), Is.EqualTo(new[]
        {
            ("weapon-a", "c1"),
            ("weapon-a", "c2"),
            ("weapon-b", "c1"),
            ("weapon-b", "c2"),
        }));
        foreach (var row in snapshot.Rows)
        {
            AssertUnknown(row);
        }

        var reversed = WeaponContactAmbiguityProjection.Project(
            new[] { contacts[1], contacts[0] },
            new[] { "weapon-a", "weapon-b" });
        Assert.That(
            WeaponContactAmbiguityFingerprint.Compute(reversed),
            Is.EqualTo(WeaponContactAmbiguityFingerprint.Compute(snapshot)));
    }

    [Test]
    public void Missing_contacts_or_weapons_yield_an_empty_snapshot()
    {
        var contact = Contact("c1", ContactProvenanceConfidence.High);

        Assert.That(
            WeaponContactAmbiguityFingerprint.Compute(WeaponContactAmbiguityProjection.Project(null, new[] { "w" })),
            Is.EqualTo("wca:empty"));
        Assert.That(
            WeaponContactAmbiguityFingerprint.Compute(WeaponContactAmbiguityProjection.Project(new[] { contact }, null)),
            Is.EqualTo("wca:empty"));
        Assert.That(
            WeaponContactAmbiguityFingerprint.Compute(
                WeaponContactAmbiguityProjection.Project(Array.Empty<ContactProvenanceState>(), new[] { "w" })),
            Is.EqualTo("wca:empty"));
        Assert.That(
            WeaponContactAmbiguityFingerprint.Compute(WeaponContactAmbiguityProjection.Project(null)),
            Is.EqualTo("wca:empty"));
    }

    [Test]
    public void Identification_state_does_not_change_the_unknown_projection()
    {
        var low = WeaponContactAmbiguityProjection.Project(
            new[] { Contact("c1", ContactProvenanceConfidence.Low) },
            new[] { "weapon-a" });
        var high = WeaponContactAmbiguityProjection.Project(
            new[] { Contact("c1", ContactProvenanceConfidence.High, lifecycle: "Identified", stale: true) },
            new[] { "weapon-a" });

        Assert.That(
            WeaponContactAmbiguityFingerprint.Compute(high),
            Is.EqualTo(WeaponContactAmbiguityFingerprint.Compute(low)));
        Assert.That(high.Rows[0].RefusalText, Is.Null);
    }

    [Test]
    public void Current_contact_and_weapon_types_have_no_ambiguity_or_tolerance_fields()
    {
        var contactNames = typeof(ContactProvenanceState).GetProperties().Select(property => property.Name).ToArray();
        var weaponNames = typeof(WeaponEnvelopeDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(contactNames, Does.Not.Contain("DownrangeAmbiguity"));
        Assert.That(contactNames, Does.Not.Contain("CrossRangeAmbiguity"));
        Assert.That(contactNames, Does.Not.Contain("WeaponTolerance"));
        Assert.That(weaponNames, Is.EqualTo(new[] { "MaxRangeMeters", "MinRangeMeters" }));
    }

    [Test]
    public void Unknown_facts_stay_unknown_and_produce_no_refusal()
    {
        var row = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", null, null, null));

        AssertUnknown(row);
        Assert.That(row.Doctrine, Is.EqualTo(AmbiguityDoctrineLevel.Unknown));
    }

    [Test]
    public void Partial_facts_stay_unknown_even_when_the_known_axis_exceeds_tolerance()
    {
        var row = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 10d, null, 2d));

        Assert.That(row.DownrangeAmbiguity.IsKnown, Is.True);
        Assert.That(row.DownrangeAmbiguity.NauticalMiles, Is.EqualTo(10d));
        Assert.That(row.CrossRangeAmbiguity.IsKnown, Is.False);
        Assert.That(row.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Unknown));
        Assert.That(row.RefusalText, Is.Null);
        Assert.That(row.Doctrine, Is.EqualTo(AmbiguityDoctrineLevel.Unknown));
    }

    [Test]
    public void Non_finite_measurements_are_unknown()
    {
        Assert.That(MeasuredNauticalMiles.FromNullable(double.NaN).IsKnown, Is.False);
        Assert.That(MeasuredNauticalMiles.FromNullable(double.PositiveInfinity).IsKnown, Is.False);

        var row = WeaponContactAmbiguityProjection.Evaluate(
            Facts("weapon-a", "c1", double.NaN, 1d, 2d));

        Assert.That(row.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Unknown));
        Assert.That(row.RefusalText, Is.Null);
    }

    [Test]
    public void Inside_and_equal_tolerance_pass_without_refusal_text()
    {
        var inside = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 1.5d, 1d, 2.5d));
        var equal = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 2d, 2d, 2d));

        Assert.That(inside.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Pass));
        Assert.That(inside.RefusalText, Is.Null);
        Assert.That(inside.Doctrine, Is.EqualTo(AmbiguityDoctrineLevel.Unknown));
        Assert.That(equal.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Pass));
        Assert.That(equal.RefusalText, Is.Null);
        Assert.That(inside, Is.EqualTo(WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 1.5d, 1d, 2.5d))));
    }

    [Test]
    public void Outside_tolerance_names_each_failing_axis_only_when_both_values_are_known()
    {
        var downrange = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 10d, 1d, 2d));
        var crossRange = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 1d, 4d, 2d));
        var both = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 10d, 4d, 2d));

        Assert.That(downrange.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Fail));
        Assert.That(downrange.RefusalText, Is.EqualTo("ambiguity 10 nm > limit 2 nm"));
        Assert.That(crossRange.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Fail));
        Assert.That(crossRange.RefusalText, Is.EqualTo("ambiguity 4 nm > limit 2 nm"));
        Assert.That(both.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Fail));
        Assert.That(
            both.RefusalText,
            Is.EqualTo("ambiguity 10 nm > limit 2 nm; ambiguity 4 nm > limit 2 nm"));
        Assert.That(both.Doctrine, Is.EqualTo(AmbiguityDoctrineLevel.Unknown));
    }

    [Test]
    public void Zero_is_a_known_measurement()
    {
        var inside = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 0d, 0d, 0d));
        var outside = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 1d, 0d, 0d));

        Assert.That(inside.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Pass));
        Assert.That(inside.RefusalText, Is.Null);
        Assert.That(outside.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Fail));
        Assert.That(outside.RefusalText, Is.EqualTo("ambiguity 1 nm > limit 0 nm"));
    }

    [Test]
    public void Supplied_facts_are_replay_stable_under_reordering()
    {
        WeaponContactAmbiguityFacts?[] facts =
        {
            Facts("weapon-b", "c1", 10d, 1d, 2d),
            Facts("weapon-a", "c2", 1d, 1d, 2d),
            null,
            Facts("weapon-a", "c2", 1d, 1d, 2d),
            Facts("weapon-a", "c1", 10d, 4d, 2d),
            Facts("", "c1", 10d, 4d, 2d),
        };
        var reversed = facts.Reverse().ToArray();

        var forward = WeaponContactAmbiguityProjection.Project(facts);
        var backward = WeaponContactAmbiguityProjection.Project(reversed);

        Assert.That(forward.Rows, Has.Count.EqualTo(3));
        Assert.That(forward.Rows[0].WeaponId, Is.EqualTo("weapon-a"));
        Assert.That(forward.Rows[0].ContactId, Is.EqualTo("c1"));
        Assert.That(forward.Rows[0].Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Fail));
        Assert.That(forward.Rows[1].Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Pass));
        Assert.That(forward.Rows[2].WeaponId, Is.EqualTo("weapon-b"));
        Assert.That(
            WeaponContactAmbiguityFingerprint.Compute(backward),
            Is.EqualTo(WeaponContactAmbiguityFingerprint.Compute(forward)));
    }

    [Test]
    public void Refusal_text_uses_invariant_decimal_formatting()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            var comma = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            comma.NumberFormat.NumberDecimalSeparator = ",";
            CultureInfo.CurrentCulture = comma;

            var row = WeaponContactAmbiguityProjection.Evaluate(Facts("weapon-a", "c1", 1.5d, 0.5d, 1d));

            Assert.That(row.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Fail));
            Assert.That(row.RefusalText, Is.EqualTo("ambiguity 1.5 nm > limit 1 nm"));
            Assert.That(row.RefusalText, Does.Not.Contain("1,5"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void Disagreeing_duplicate_facts_stay_distinct_and_ordered()
    {
        var first = WeaponContactAmbiguityProjection.Project(new[]
        {
            Facts("weapon-a", "c1", 10d, 1d, 2d),
            Facts("weapon-a", "c1", 1d, 1d, 2d),
        });
        var second = WeaponContactAmbiguityProjection.Project(new[]
        {
            Facts("weapon-a", "c1", 1d, 1d, 2d),
            Facts("weapon-a", "c1", 10d, 1d, 2d),
        });

        Assert.That(first.Rows, Has.Count.EqualTo(2));
        Assert.That(first.Rows[0].Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Pass));
        Assert.That(first.Rows[1].Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Fail));
        Assert.That(
            WeaponContactAmbiguityFingerprint.Compute(second),
            Is.EqualTo(WeaponContactAmbiguityFingerprint.Compute(first)));
    }

    [Test]
    public void New_sources_stay_inside_the_headless_projection_fence()
    {
        foreach (var fileName in ProductionSources)
        {
            var source = File.ReadAllText(ProjectionPath(fileName));
            foreach (var token in ForbiddenTokens)
            {
                Assert.That(source, Does.Not.Contain(token), $"{fileName} must not reference {token}.");
            }

            Assert.That(source, Does.Not.Contain(".Confidence"), $"{fileName} must not read identification confidence.");
            Assert.That(source, Does.Not.Contain("MaxRangeMeters"), $"{fileName} must not treat weapon range as tolerance.");
            Assert.That(source, Does.Not.Contain("MinRangeMeters"));
            Assert.That(source, Does.Not.Contain("DefaultWeaponRangeNm"));
        }
    }

    [Test]
    public void Evaluate_rejects_a_null_fact()
    {
        Assert.Throws<ArgumentNullException>(() => WeaponContactAmbiguityProjection.Evaluate(null!));
    }

    private static void AssertUnknown(WeaponContactAmbiguityRow row)
    {
        Assert.That(row.DownrangeAmbiguity, Is.EqualTo(MeasuredNauticalMiles.Unknown));
        Assert.That(row.CrossRangeAmbiguity, Is.EqualTo(MeasuredNauticalMiles.Unknown));
        Assert.That(row.WeaponTolerance, Is.EqualTo(MeasuredNauticalMiles.Unknown));
        Assert.That(row.Doctrine, Is.EqualTo(AmbiguityDoctrineLevel.Unknown));
        Assert.That(row.Verdict, Is.EqualTo(AmbiguityToleranceVerdict.Unknown));
        Assert.That(row.RefusalText, Is.Null);
    }

    private static WeaponContactAmbiguityFacts Facts(
        string weaponId,
        string contactId,
        double? downrangeNm,
        double? crossRangeNm,
        double? toleranceNm) =>
        new(weaponId, contactId, downrangeNm, crossRangeNm, toleranceNm, AmbiguityDoctrineLevel.Unknown);

    private static ContactProvenanceState Contact(
        string contactId,
        ContactProvenanceConfidence confidence,
        string lifecycle = "Detected",
        bool stale = false) =>
        new(
            contactId,
            new ContactProvenanceSource("observer", "target", "observer:observer|target:target"),
            confidence,
            stale ? ContactProvenanceFreshness.Stale : ContactProvenanceFreshness.Fresh,
            stale ? 40UL : 0UL,
            new ContactProvenanceLastKnown(lifecycle, "target", 1UL, 1d),
            false,
            stale ? ContactProvenanceQualityState.Stale : ContactProvenanceQualityState.None);

    private static string ProjectionPath(string fileName)
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "repo root not found");
        return Path.Combine(
            dir!.FullName,
            "src",
            "ProjectAegis.Delegation",
            "Projection",
            fileName);
    }
}
