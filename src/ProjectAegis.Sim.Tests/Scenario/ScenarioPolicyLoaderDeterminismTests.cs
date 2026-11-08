using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

/// <summary>
/// DRG-54 / ADR-022 Decision 3: scenario policy directory loading must be
/// deterministic across hosts, and a duplicate id must fail loudly rather than
/// silently resolving last-writer-wins in filesystem order.
/// </summary>
public sealed class ScenarioPolicyLoaderDeterminismTests
{
    private const string PolicyTemplate =
        "{\"id\":\"{ID}\",\"description\":\"drg-54 fixture\",\"engage\":{\"maxSalvo\":2}}";

    [Fact]
    public void Loader_rejects_duplicate_policy_id_naming_both_files()
    {
        using var dir = new TempPolicyDirectory();
        dir.WritePolicy("alpha.policy.json", "dup-id");
        dir.WritePolicy("beta.policy.json", "dup-id");

        var ex = Assert.Throws<InvalidDataException>(
            () => ScenarioPolicyJsonLoader.LoadDirectory(dir.Path));

        // Both filenames must appear so the author can find the collision.
        Assert.Contains("dup-id", ex.Message, StringComparison.Ordinal);
        Assert.Contains("alpha.policy.json", ex.Message, StringComparison.Ordinal);
        Assert.Contains("beta.policy.json", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Index_rejects_duplicate_policy_id_naming_both_files()
    {
        using var dir = new TempPolicyDirectory();
        dir.WritePolicy("alpha.policy.json", "dup-id");
        dir.WritePolicy("beta.policy.json", "dup-id");

        var ex = Assert.Throws<InvalidDataException>(
            () => ScenarioPolicyJsonIndex.LoadFromDirectory(dir.Path));

        Assert.Contains("dup-id", ex.Message, StringComparison.Ordinal);
        Assert.Contains("alpha.policy.json", ex.Message, StringComparison.Ordinal);
        Assert.Contains("beta.policy.json", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Duplicate_detection_is_case_insensitive_matching_the_map_comparer()
    {
        // The profile map uses OrdinalIgnoreCase, so ids differing only by case
        // collide there. Detection must use the same comparer or the collision
        // slips through and one profile is silently dropped.
        using var dir = new TempPolicyDirectory();
        dir.WritePolicy("alpha.policy.json", "Case-Id");
        dir.WritePolicy("beta.policy.json", "case-id");

        Assert.Throws<InvalidDataException>(
            () => ScenarioPolicyJsonLoader.LoadDirectory(dir.Path));
    }

    [Fact]
    public void Distinct_ids_load_regardless_of_filename_order()
    {
        // Filenames deliberately sort in the opposite order to their ids, so a
        // host-order dependency would show up as a differing result set.
        using var dir = new TempPolicyDirectory();
        dir.WritePolicy("zzz-first.policy.json", "aaa-id");
        dir.WritePolicy("aaa-second.policy.json", "zzz-id");

        var map = ScenarioPolicyJsonLoader.LoadDirectory(dir.Path);

        Assert.Equal(2, map.Count);
        Assert.True(map.ContainsKey("aaa-id"));
        Assert.True(map.ContainsKey("zzz-id"));
    }

    [Fact]
    public void Empty_or_missing_directory_is_not_an_error()
    {
        using var dir = new TempPolicyDirectory();
        Assert.Empty(ScenarioPolicyJsonLoader.LoadDirectory(dir.Path));
        Assert.Empty(ScenarioPolicyJsonLoader.LoadDirectory(
            Path.Combine(dir.Path, "does-not-exist")));
    }

    private sealed class TempPolicyDirectory : IDisposable
    {
        public TempPolicyDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "aegis-drg54-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void WritePolicy(string fileName, string id) =>
            File.WriteAllText(
                System.IO.Path.Combine(Path, fileName),
                PolicyTemplate.Replace("{ID}", id, StringComparison.Ordinal));

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Test cleanup only — a locked temp dir must not fail the run.
            }
        }
    }
}
