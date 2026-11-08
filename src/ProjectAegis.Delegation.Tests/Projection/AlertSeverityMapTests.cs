using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Phase 0 contract (req 20 §Alerting and Interruption, TR-c2-007): category → severity tier
/// mapping. Locks the agreed 2026-07-08 table, including WEAPON_LAUNCH → Routine.</summary>
[TestFixture]
public sealed class AlertSeverityMapTests
{
    [TestCase("KILL_CONFIRMED")]
    [TestCase("POLICY_DENIAL")]
    public void Critical_categories_map_to_critical(string category)
    {
        Assert.That(AlertSeverityMap.ForCategory(category), Is.EqualTo(AlertSeverity.Critical));
    }

    [TestCase("CONTACT")]
    [TestCase("MODE")]
    [TestCase("MISSION")]
    [TestCase("ENGAGE_ABORT")]
    public void Notable_categories_map_to_notable(string category)
    {
        Assert.That(AlertSeverityMap.ForCategory(category), Is.EqualTo(AlertSeverity.Notable));
    }

    [TestCase("MAGAZINE")]
    [TestCase("PLAYER_ORDER")]
    [TestCase("COMMS")]
    [TestCase("FUEL")]
    [TestCase("INTERCEPT_SUCCESS")]
    [TestCase("HIT")]
    [TestCase("MISS")]
    [TestCase("COMBAT")]
    public void Routine_categories_map_to_routine(string category)
    {
        Assert.That(AlertSeverityMap.ForCategory(category), Is.EqualTo(AlertSeverity.Routine));
    }

    [Test]
    public void Weapon_launch_is_routine_by_decision()
    {
        // 2026-07-08: WEAPON_LAUNCH fires on friendly launches too; inbound-threat criticality is
        // carried by KILL_CONFIRMED / POLICY_DENIAL.
        Assert.That(AlertSeverityMap.ForCategory("WEAPON_LAUNCH"), Is.EqualTo(AlertSeverity.Routine));
    }

    [TestCase("")]
    [TestCase("SOMETHING_NEW")]
    [TestCase(null)]
    public void Unknown_or_null_defaults_to_routine(string? category)
    {
        Assert.That(AlertSeverityMap.ForCategory(category), Is.EqualTo(AlertSeverity.Routine));
    }

    [TestCase("kill_confirmed", AlertSeverity.Critical)]
    [TestCase("Contact", AlertSeverity.Notable)]
    [TestCase("weapon_launch", AlertSeverity.Routine)]
    public void Mapping_is_case_insensitive(string category, AlertSeverity expected)
    {
        Assert.That(AlertSeverityMap.ForCategory(category), Is.EqualTo(expected));
    }
}
