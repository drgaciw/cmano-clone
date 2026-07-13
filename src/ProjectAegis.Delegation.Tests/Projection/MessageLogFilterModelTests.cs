using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Track T3 (req 20 §Message log filters, TR-c2-008): pure per-category filter — category set →
/// predicate over <see cref="MessageLogLine"/>.</summary>
[TestFixture]
public sealed class MessageLogFilterModelTests
{
    [Test]
    public void All_categories_enabled_by_default()
    {
        var filter = new MessageLogFilterModel();

        Assert.That(filter.IsEnabled("KILL_CONFIRMED"), Is.True);
        Assert.That(filter.DisabledCategories, Is.Empty);
    }

    [Test]
    public void Disabling_a_category_removes_matching_lines_from_apply()
    {
        var filter = new MessageLogFilterModel();
        filter.SetEnabled("FUEL", enabled: false);

        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "KILL_CONFIRMED", "a", "u1"),
            new MessageLogLine(2, 2.0, "FUEL", "b", "u2"),
            new MessageLogLine(3, 3.0, "FUEL", "c", "u3"),
        };

        var result = filter.Apply(lines);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Category, Is.EqualTo("KILL_CONFIRMED"));
    }

    [Test]
    public void Apply_with_no_disabled_categories_returns_input_unchanged()
    {
        var filter = new MessageLogFilterModel();
        var lines = new[] { new MessageLogLine(1, 1.0, "COMMS", "a", "u1") };

        Assert.That(filter.Apply(lines), Is.SameAs(lines));
    }

    [Test]
    public void Toggle_flips_state_and_returns_new_state()
    {
        var filter = new MessageLogFilterModel();

        var afterFirstToggle = filter.Toggle("MODE");
        Assert.That(afterFirstToggle, Is.False);
        Assert.That(filter.IsEnabled("MODE"), Is.False);

        var afterSecondToggle = filter.Toggle("MODE");
        Assert.That(afterSecondToggle, Is.True);
        Assert.That(filter.IsEnabled("MODE"), Is.True);
    }

    [Test]
    public void Reset_re_enables_every_category()
    {
        var filter = new MessageLogFilterModel();
        filter.SetEnabled("FUEL", enabled: false);
        filter.SetEnabled("COMMS", enabled: false);

        filter.Reset();

        Assert.That(filter.DisabledCategories, Is.Empty);
        Assert.That(filter.IsEnabled("FUEL"), Is.True);
        Assert.That(filter.IsEnabled("COMMS"), Is.True);
    }

    [Test]
    public void Matches_reflects_current_filter_state_for_a_single_line()
    {
        var filter = new MessageLogFilterModel();
        var line = new MessageLogLine(1, 1.0, "CONTACT", "text", "u1");

        Assert.That(filter.Matches(line), Is.True);

        filter.SetEnabled("CONTACT", enabled: false);

        Assert.That(filter.Matches(line), Is.False);
    }

    [Test]
    public void Disabling_all_present_categories_yields_empty_apply_result()
    {
        var filter = new MessageLogFilterModel();
        filter.SetEnabled("FUEL", enabled: false);

        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "FUEL", "a", "u1"),
            new MessageLogLine(2, 2.0, "FUEL", "b", "u2"),
        };

        Assert.That(filter.Apply(lines), Is.Empty);
    }

    [Test]
    public void Category_matching_is_case_sensitive()
    {
        // MessageLogProjection categories are fixed all-caps identifiers (see AlertSeverityMap); the
        // filter must match them exactly rather than silently folding case, so a lowercase disable
        // request does not accidentally match — or fail to match — a differently-cased category.
        var filter = new MessageLogFilterModel();
        filter.SetEnabled("fuel", enabled: false);

        Assert.That(filter.IsEnabled("FUEL"), Is.True, "disabling 'fuel' must not disable 'FUEL'");

        var lines = new[] { new MessageLogLine(1, 1.0, "FUEL", "a", "u1") };
        Assert.That(filter.Apply(lines), Has.Count.EqualTo(1));
    }

    [Test]
    public void Toggling_a_category_never_seen_in_any_line_does_not_affect_other_categories()
    {
        var filter = new MessageLogFilterModel();

        filter.SetEnabled("NOT_A_REAL_CATEGORY", enabled: false);

        var lines = new[] { new MessageLogLine(1, 1.0, "COMMS", "a", "u1") };
        Assert.That(filter.Apply(lines), Has.Count.EqualTo(1));
        Assert.That(filter.DisabledCategories, Has.Count.EqualTo(1));
    }
}
