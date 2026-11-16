using Xunit;

namespace ProjectAegis.Ai.LiveEval;

/// <summary>AC: LiveEval stays outside ProjectAegis.sln / CI.</summary>
public sealed class LiveEvalIsolationGateTests
{
    [Fact]
    public void ProjectAegis_sln_does_not_reference_LiveEval()
    {
        var sln = FindSolutionFile();
        var text = File.ReadAllText(sln);
        Assert.DoesNotContain("ProjectAegis.Ai.LiveEval", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ai.LiveEval", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LiveEval_csproj_does_not_pin_paid_provider_or_evaluation_packages()
    {
        var csproj = Path.Combine(FindLiveEvalRoot(), "ProjectAegis.Ai.LiveEval.csproj");
        var text = File.ReadAllText(csproj);
        Assert.DoesNotContain("Microsoft.Extensions.AI.Evaluation", text, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Anthropic", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Azure.AI", text, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindSolutionFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "ProjectAegis.sln");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("ProjectAegis.sln not found from " + AppContext.BaseDirectory);
    }

    private static string FindLiveEvalRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "ProjectAegis.Ai.LiveEval");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            if (string.Equals(dir.Name, "ProjectAegis.Ai.LiveEval", StringComparison.Ordinal)
                && File.Exists(Path.Combine(dir.FullName, "ProjectAegis.Ai.LiveEval.csproj")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("ProjectAegis.Ai.LiveEval not found");
    }
}
