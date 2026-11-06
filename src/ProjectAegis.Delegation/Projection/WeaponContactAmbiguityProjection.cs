namespace ProjectAegis.Delegation.Projection;

using System.Globalization;

/// <summary>
/// DRG-411: headless (weapon, contact) ambiguity versus weapon tolerance.
/// Contact provenance exposes identity, freshness, age, and comms. Weapon envelopes
/// expose min/max range. Neither exposes downrange ambiguity, cross-range ambiguity,
/// or launch tolerance, and no ambiguity-doctrine producer exists (DRG-385).
/// Rows built from provenance and weapon ids therefore stay unknown.
/// Numeric pass/fail runs only from <see cref="WeaponContactAmbiguityFacts"/>
/// supplied by a producer or by a test. Doctrine is reported as supplied and is not
/// a scale factor: a known tolerance is the limit itself.
/// </summary>
public static class WeaponContactAmbiguityProjection
{
    /// <summary>
    /// One unknown row per distinct weapon id and contact id.
    /// Provenance fields other than the contact id are not measurements.
    /// </summary>
    public static WeaponContactAmbiguitySnapshot Project(
        IReadOnlyList<ContactProvenanceState>? contacts,
        IReadOnlyList<string>? weaponIds)
    {
        var weapons = DistinctSorted(weaponIds);
        var contactIds = DistinctContactIds(contacts);
        if (weapons.Length == 0 || contactIds.Length == 0)
        {
            return WeaponContactAmbiguitySnapshot.Empty;
        }

        var rows = new WeaponContactAmbiguityRow[weapons.Length * contactIds.Length];
        var index = 0;
        for (var w = 0; w < weapons.Length; w++)
        {
            for (var c = 0; c < contactIds.Length; c++)
            {
                rows[index++] = UnknownRow(weapons[w], contactIds[c]);
            }
        }

        return new WeaponContactAmbiguitySnapshot(rows);
    }

    /// <summary>
    /// Evaluates caller-supplied facts. Null entries and blank ids are skipped.
    /// Output order is ordinal by weapon id, then contact id, then row contents.
    /// Identical rows collapse to one.
    /// </summary>
    public static WeaponContactAmbiguitySnapshot Project(
        IReadOnlyList<WeaponContactAmbiguityFacts?>? facts)
    {
        if (facts is null || facts.Count == 0)
        {
            return WeaponContactAmbiguitySnapshot.Empty;
        }

        var rows = new List<WeaponContactAmbiguityRow>(facts.Count);
        for (var i = 0; i < facts.Count; i++)
        {
            var fact = facts[i];
            if (fact is null || string.IsNullOrEmpty(fact.WeaponId) || string.IsNullOrEmpty(fact.ContactId))
            {
                continue;
            }

            rows.Add(Evaluate(fact));
        }

        if (rows.Count == 0)
        {
            return WeaponContactAmbiguitySnapshot.Empty;
        }

        rows.Sort(CompareRows);
        var unique = new List<WeaponContactAmbiguityRow>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            if (unique.Count == 0 || CompareRows(unique[unique.Count - 1], rows[i]) != 0)
            {
                unique.Add(rows[i]);
            }
        }

        return new WeaponContactAmbiguitySnapshot(unique.ToArray());
    }

    /// <summary>
    /// Evaluates one fact. Null measurements stay unknown, and an unknown measurement
    /// forces <see cref="AmbiguityToleranceVerdict.Unknown"/> with no refusal text.
    /// </summary>
    public static WeaponContactAmbiguityRow Evaluate(WeaponContactAmbiguityFacts facts)
    {
        if (facts is null)
        {
            throw new ArgumentNullException(nameof(facts));
        }

        var downrange = MeasuredNauticalMiles.FromNullable(facts.DownrangeAmbiguityNm);
        var crossRange = MeasuredNauticalMiles.FromNullable(facts.CrossRangeAmbiguityNm);
        var tolerance = MeasuredNauticalMiles.FromNullable(facts.WeaponToleranceNm);
        var judged = Judge(downrange, crossRange, tolerance);
        return new WeaponContactAmbiguityRow(
            facts.WeaponId,
            facts.ContactId,
            downrange,
            crossRange,
            tolerance,
            facts.Doctrine,
            judged.Verdict,
            judged.RefusalText);
    }

    private static WeaponContactAmbiguityRow UnknownRow(string weaponId, string contactId) =>
        new(
            weaponId,
            contactId,
            MeasuredNauticalMiles.Unknown,
            MeasuredNauticalMiles.Unknown,
            MeasuredNauticalMiles.Unknown,
            AmbiguityDoctrineLevel.Unknown,
            AmbiguityToleranceVerdict.Unknown,
            null);

    private static (AmbiguityToleranceVerdict Verdict, string? RefusalText) Judge(
        MeasuredNauticalMiles downrange,
        MeasuredNauticalMiles crossRange,
        MeasuredNauticalMiles tolerance)
    {
        if (!downrange.TryGetNauticalMiles(out var downNm)
            || !crossRange.TryGetNauticalMiles(out var crossNm)
            || !tolerance.TryGetNauticalMiles(out var limitNm))
        {
            return (AmbiguityToleranceVerdict.Unknown, null);
        }

        var downPhrase = downNm > limitNm ? Phrase(downNm, limitNm) : null;
        var crossPhrase = crossNm > limitNm ? Phrase(crossNm, limitNm) : null;
        if (downPhrase is null && crossPhrase is null)
        {
            return (AmbiguityToleranceVerdict.Pass, null);
        }

        if (downPhrase is not null && crossPhrase is not null)
        {
            return (AmbiguityToleranceVerdict.Fail, downPhrase + "; " + crossPhrase);
        }

        return (AmbiguityToleranceVerdict.Fail, downPhrase ?? crossPhrase);
    }

    private static string Phrase(double ambiguityNm, double limitNm)
    {
        var ambiguity = FormatNauticalMiles(ambiguityNm);
        var limit = FormatNauticalMiles(limitNm);
        return $"ambiguity {ambiguity} nm > limit {limit} nm";
    }

    internal static string FormatNauticalMiles(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private static int CompareRows(WeaponContactAmbiguityRow left, WeaponContactAmbiguityRow right)
    {
        var weapon = string.CompareOrdinal(left.WeaponId, right.WeaponId);
        if (weapon != 0)
        {
            return weapon;
        }

        var contact = string.CompareOrdinal(left.ContactId, right.ContactId);
        if (contact != 0)
        {
            return contact;
        }

        return string.CompareOrdinal(
            WeaponContactAmbiguityFingerprint.RowKey(left),
            WeaponContactAmbiguityFingerprint.RowKey(right));
    }

    private static string[] DistinctContactIds(IReadOnlyList<ContactProvenanceState>? contacts)
    {
        if (contacts is null || contacts.Count == 0)
        {
            return Array.Empty<string>();
        }

        var ids = new string[contacts.Count];
        var count = 0;
        for (var i = 0; i < contacts.Count; i++)
        {
            var contact = contacts[i];
            if (string.IsNullOrEmpty(contact.ContactId))
            {
                continue;
            }

            ids[count++] = contact.ContactId;
        }

        if (count == 0)
        {
            return Array.Empty<string>();
        }

        if (count != ids.Length)
        {
            Array.Resize(ref ids, count);
        }

        return DistinctSorted(ids);
    }

    private static string[] DistinctSorted(IReadOnlyList<string>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return Array.Empty<string>();
        }

        var set = new SortedSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            if (!string.IsNullOrEmpty(id))
            {
                set.Add(id);
            }
        }

        if (set.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new string[set.Count];
        set.CopyTo(result);
        return result;
    }
}
