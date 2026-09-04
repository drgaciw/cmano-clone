using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>
/// S122-13 host source contract: paused Play skips Update RunTick, so Start must
/// RunTick once after seed/select/optional BeginExecution to fill LastSensorC2.
/// </summary>
[TestFixture]
public sealed class SimplePlayModeSimHostContractTests
{
    [Test]
    public void Start_calls_run_tick_after_select_contact_so_paused_play_fills_last_sensor_c2()
    {
        var source = File.ReadAllText(RequireHostPath());
        var start = ExtractMethodBody(source, "private void Start()");

        Assert.That(start, Does.Contain("SelectContact(SmokeContactId)"));
        Assert.That(
            start,
            Does.Contain("bridgeHost.RunTick(this, this)"),
            "Start() must call bridgeHost.RunTick(this, this) after SelectContact so paused Play fills LastSensorC2.");

        var selectIndex = start.IndexOf("SelectContact(SmokeContactId)", StringComparison.Ordinal);
        var beginIndex = start.IndexOf("BeginExecution()", StringComparison.Ordinal);
        var tickIndex = start.LastIndexOf("bridgeHost.RunTick(this, this)", StringComparison.Ordinal);

        Assert.That(selectIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(tickIndex, Is.GreaterThan(selectIndex), "RunTick must follow SelectContact.");
        Assert.That(beginIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(tickIndex, Is.GreaterThan(beginIndex), "RunTick must follow optional BeginExecution.");
        Assert.That(
            CountOccurrences(start, "bridgeHost.RunTick(this, this)"),
            Is.EqualTo(1),
            "Start() must RunTick exactly once.");
    }

    private static string RequireHostPath()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "unity",
                "ProjectAegis",
                "Assets",
                "Scripts",
                "Runtime",
                "SimplePlayModeSimHost.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("SimplePlayModeSimHost.cs not found from test directory.");
    }

    private static string ExtractMethodBody(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Missing {signature}");
        var brace = source.IndexOf('{', start);
        Assert.That(brace, Is.GreaterThan(start));
        var depth = 0;
        for (var i = brace; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(brace, i - brace + 1);
                }
            }
        }

        throw new InvalidOperationException($"Unbalanced braces for {signature}.");
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
