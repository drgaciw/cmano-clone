namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

public sealed class ScenarioEditVersionGuardTests
{
    [Test]
    public void Matching_editVersion_returns_no_conflict()
    {
        Assert.That(ScenarioEditVersionGuard.TryCheck(3, 3), Is.Null);
    }

    [Test]
    public void Stale_editVersion_returns_conflict_with_current_version()
    {
        var conflict = ScenarioEditVersionGuard.TryCheck(2, 5, "abc123");
        Assert.That(conflict, Is.Not.Null);
        Assert.That(conflict!.Code, Is.EqualTo("CONFLICT"));
        Assert.That(conflict.CurrentEditVersion, Is.EqualTo(5));
        Assert.That(conflict.FileHash, Is.EqualTo("abc123"));
    }
}