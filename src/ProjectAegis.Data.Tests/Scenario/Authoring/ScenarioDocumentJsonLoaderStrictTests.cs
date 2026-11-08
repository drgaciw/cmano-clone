using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario.Authoring;

/// <summary>DRG-694: opt-in strict JSON (duplicate properties rejected) for scenario documents.</summary>
public sealed class ScenarioDocumentJsonLoaderStrictTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aegis-strict-json-" + Guid.NewGuid().ToString("N"));

    public ScenarioDocumentJsonLoaderStrictTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Write(string json)
    {
        var path = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    private const string Duplicate = """{ "metadata": {}, "sides": [], "sides": [] }""";
    private const string CaseDuplicate = """{ "metadata": {}, "Metadata": {} }""";
    private const string Clean = """{ /* ok */ "metadata": {}, "sides": [], }""";

    [Fact]
    public void Default_load_still_tolerates_duplicate_properties()
    {
        var doc = ScenarioDocumentJsonLoader.LoadFromFile(Write(Duplicate));
        Assert.NotNull(doc);
    }

    [Fact]
    public void Strict_load_rejects_duplicate_properties()
    {
        var ex = Assert.Throws<InvalidDataException>(() => ScenarioDocumentJsonLoader.LoadFromFile(Write(Duplicate), strict: true));
        Assert.Contains("strict", ex.Message);
    }

    [Fact]
    public void Strict_load_rejects_case_insensitive_duplicates()
    {
        Assert.Throws<InvalidDataException>(() => ScenarioDocumentJsonLoader.LoadFromFile(Write(CaseDuplicate), strict: true));
    }

    [Fact]
    public void Strict_load_keeps_comments_and_trailing_commas()
    {
        var doc = ScenarioDocumentJsonLoader.LoadFromFile(Write(Clean), strict: true);
        Assert.Empty(doc.Sides);
    }

    [Fact]
    public void Strict_load_accepts_repo_sample_scenarios()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "ProjectAegis.sln")))
            root = Path.GetDirectoryName(root);
        Assert.NotNull(root);
        var files = Directory.EnumerateFiles(Path.Combine(root, "data"), "*.scenario.json", SearchOption.AllDirectories).Take(25).ToList();
        foreach (var f in files)
            ScenarioDocumentJsonLoader.LoadFromFile(f, strict: true);
    }
}
