using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class MissionPlanSuggestCommandTests
{
    [Fact]
    public void Patrol_and_strike_intent_returns_both_suggestions()
    {
        using var writer = new StringWriter();
        Assert.Equal(0, MissionPlanSuggestCommand.Run("patrol baltic then strike hostile-1", writer));
        var json = writer.ToString();
        Assert.Contains("mission_add_patrol", json, StringComparison.Ordinal);
        Assert.Contains("mission_add_strike", json, StringComparison.Ordinal);
    }
}