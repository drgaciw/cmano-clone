namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

public sealed class ScenarioEditVersionGuardTests
{
    [Fact]
    public void Matching_editVersion_returns_no_conflict()
    {
        Assert.Null(ScenarioEditVersionGuard.TryCheck(3, 3));
    }

    [Fact]
    public void Stale_editVersion_returns_conflict_with_current_version()
    {
        var conflict = ScenarioEditVersionGuard.TryCheck(2, 5, "abc123");
        Assert.NotNull(conflict);
        Assert.Equal("CONFLICT", conflict!.Code);
        Assert.Equal(5, conflict.CurrentEditVersion);
        Assert.Equal("abc123", conflict.FileHash);
    }
}