using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0: OOB human/agent ownership filters.</summary>
[TestFixture]
public sealed class DelegationOobFilterTests
{
    private static DelegationStateProjection Row(string id, DelegationOwnerKind owner) =>
        new(id, owner, AutonomyLevel.FullAutonomous, PersonalityId: "", Paused: false);

    [Test]
    public void Filter_All_returns_input()
    {
        var rows = new[]
        {
            Row("h", DelegationOwnerKind.Human),
            Row("a", DelegationOwnerKind.Agent),
            Row("m", DelegationOwnerKind.Mixed),
        };

        Assert.That(DelegationOobFilter.Filter(rows, DelegationOobFilterMode.All), Is.SameAs(rows));
    }

    [Test]
    public void Filter_HumanOnly_includes_human_and_mixed()
    {
        var rows = new[]
        {
            Row("h", DelegationOwnerKind.Human),
            Row("a", DelegationOwnerKind.Agent),
            Row("m", DelegationOwnerKind.Mixed),
        };

        var filtered = DelegationOobFilter.Filter(rows, DelegationOobFilterMode.HumanOnly);

        Assert.That(filtered.Select(r => r.UnitId).ToArray(), Is.EqualTo(new[] { "h", "m" }));
    }

    [Test]
    public void Filter_AgentOnly_includes_agent_and_mixed()
    {
        var rows = new[]
        {
            Row("h", DelegationOwnerKind.Human),
            Row("a", DelegationOwnerKind.Agent),
            Row("m", DelegationOwnerKind.Mixed),
        };

        var filtered = DelegationOobFilter.Filter(rows, DelegationOobFilterMode.AgentOnly);

        Assert.That(filtered.Select(r => r.UnitId).ToArray(), Is.EqualTo(new[] { "a", "m" }));
    }

    [Test]
    public void FilterOob_applies_ownership_map()
    {
        var oob = new[]
        {
            new OobTreeEntry("h", IsAlive: true),
            new OobTreeEntry("a", IsAlive: true),
            new OobTreeEntry("unknown", IsAlive: false),
        };
        var ownership = new Dictionary<string, DelegationOwnerKind>(StringComparer.Ordinal)
        {
            ["h"] = DelegationOwnerKind.Human,
            ["a"] = DelegationOwnerKind.Agent,
        };

        var human = DelegationOobFilter.FilterOob(oob, ownership, DelegationOobFilterMode.HumanOnly);
        Assert.That(human.Select(r => r.UnitId).ToArray(), Is.EqualTo(new[] { "h" }));

        var agent = DelegationOobFilter.FilterOob(oob, ownership, DelegationOobFilterMode.AgentOnly);
        Assert.That(agent.Select(r => r.UnitId).ToArray(), Is.EqualTo(new[] { "a" }));
    }
}
