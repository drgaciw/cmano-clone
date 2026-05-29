namespace ProjectAegis.Delegation.Tests.Decision;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class DeterminismTests
{
    [Test]
    public void SeededRng_same_seed_produces_same_sequence()
    {
        var a = new SeededRng(9001, agentSalt: 42);
        var b = new SeededRng(9001, agentSalt: 42);
        Assert.That(a.NextUnit(), Is.EqualTo(b.NextUnit()));
        Assert.That(a.NextUnit(), Is.EqualTo(b.NextUnit()));
    }

    [Test]
    public void PersonalityCatalog_exposes_six_presets()
    {
        var names = PersonalityCatalog.All.Select(p => p.Name).ToArray();
        Assert.That(names, Has.Length.EqualTo(6));
        Assert.That(names, Does.Contain("Aggressive"));
        Assert.That(names, Does.Contain("SwarmCoordinator"));
    }
}
