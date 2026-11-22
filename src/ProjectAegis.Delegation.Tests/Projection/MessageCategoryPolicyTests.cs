using NUnit.Framework;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class MessageCategoryPolicyTests
{
    private static readonly string[] ExpectedCategories =
    {
        "KILL_CONFIRMED",
        "INTERCEPT_SUCCESS",
        "HIT",
        "MISS",
        "COMBAT",
        "WEAPON_LAUNCH",
        "ENGAGE_ABORT",
        "POLICY_DENIAL",
        "CONTACT",
        "MAGAZINE",
        "MODE",
        "PLAYER_ORDER",
        "COMMS",
        "FUEL",
        "ORDNANCE",
        "POLICY_UPDATE",
        "AGENT_DECISION",
        "MISSION",
        "EVENT",
        "DAMAGE",
        "CONTROLLER",
    };

    [Test]
    public void LoadDefault_loads_data_driven_json_for_all_MessageLogProjection_categories()
    {
        var policy = MessageCategoryPolicy.LoadDefault();

        Assert.That(policy.Categories.Count, Is.GreaterThanOrEqualTo(ExpectedCategories.Length));
        foreach (var category in ExpectedCategories)
        {
            Assert.That(policy.Categories, Does.Contain(category), $"Missing default rule for {category}");
            var rule = policy.GetRule(category);
            Assert.That(rule.Log, Is.True, $"{category} should default Log=true");
        }

        Assert.That(policy.GetRule("DAMAGE"), Is.EqualTo(new MessageCategoryRule(true, true, true, true)));
        Assert.That(policy.GetRule("CONTACT"), Is.EqualTo(new MessageCategoryRule(true, true, true, true)));
        Assert.That(policy.GetRule("WEAPON_LAUNCH"), Is.EqualTo(new MessageCategoryRule(true, false, true, true)));
        Assert.That(policy.GetRule("KILL_CONFIRMED"), Is.EqualTo(new MessageCategoryRule(true, false, true, true)));
        Assert.That(policy.GetRule("HIT"), Is.EqualTo(new MessageCategoryRule(true, false, true, false)));
        Assert.That(policy.GetRule("AGENT_DECISION"), Is.EqualTo(new MessageCategoryRule(true, false, false, false)));
    }

    [Test]
    public void FromJson_parses_custom_rules_and_WithRule_returns_updated_policy_without_mutating_prior()
    {
        const string json = """
        {
          "version": 1,
          "fallbackRule": { "log": true, "pause": false, "mapBalloon": false, "dropToRealTime": false },
          "categories": {
            "FUEL": { "log": true, "pause": true, "mapBalloon": true, "dropToRealTime": true }
          }
        }
        """;

        var parsed = MessageCategoryPolicy.FromJson(json);
        Assert.That(parsed.GetRule("FUEL"), Is.EqualTo(new MessageCategoryRule(true, true, true, true)));
        Assert.That(parsed.GetRule("UNKNOWN_CUSTOM"), Is.EqualTo(MessageCategoryRule.DefaultLogOnly));

        var updated = parsed.WithRule("FUEL", new MessageCategoryRule(false, false, true, false));
        Assert.That(parsed.GetRule("FUEL").Pause, Is.True, "Prior policy instance must remain immutable.");
        Assert.That(updated.GetRule("FUEL"), Is.EqualTo(new MessageCategoryRule(false, false, true, false)));
        Assert.That(updated.GetRule("FUEL").ToActions(), Is.EqualTo(MessageNotificationActions.MapBalloon));
    }

    [Test]
    public void GetRule_normalizes_case_and_falls_back_for_unknown_category()
    {
        var policy = MessageCategoryPolicy.LoadDefault();

        Assert.That(policy.GetRule(" damage "), Is.EqualTo(policy.GetRule("DAMAGE")));
        Assert.That(policy.GetRule(null), Is.EqualTo(policy.FallbackRule));
        Assert.That(policy.GetRule("UNMAPPED_CATEGORY"), Is.EqualTo(policy.FallbackRule));
    }
}
