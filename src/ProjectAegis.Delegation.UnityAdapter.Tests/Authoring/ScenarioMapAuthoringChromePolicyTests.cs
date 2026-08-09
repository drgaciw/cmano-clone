using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// Pure chrome enablement policy for Scenario Map Authoring host.
/// Catalog/form stay interactive pre-session; write actions require an open scenario.
/// </summary>
[TestFixture]
public sealed class ScenarioMapAuthoringChromePolicyTests
{
    [Test]
    public void ShouldEnableCatalogAndFormChrome_is_true_with_and_without_session()
    {
        Assert.That(ScenarioMapAuthoringHostPolicy.ShouldEnableCatalogAndFormChrome(hasSession: false), Is.True);
        Assert.That(ScenarioMapAuthoringHostPolicy.ShouldEnableCatalogAndFormChrome(hasSession: true), Is.True);
    }

    [Test]
    public void ShouldEnableSessionWriteActions_requires_open_session()
    {
        Assert.That(ScenarioMapAuthoringHostPolicy.ShouldEnableSessionWriteActions(hasSession: false), Is.False);
        Assert.That(ScenarioMapAuthoringHostPolicy.ShouldEnableSessionWriteActions(hasSession: true), Is.True);
    }

    [Test]
    public void NoSessionMessage_is_actionable_and_mentions_open_path()
    {
        var message = ScenarioMapAuthoringHostPolicy.NoSessionMessage;
        Assert.That(message, Is.Not.Null.And.Not.Empty);

        var lower = message.ToLowerInvariant();
        var mentionsOpenOrBrowse = lower.Contains("open") || lower.Contains("browse");
        var mentionsScenarioJson = lower.Contains("scenario.json");
        Assert.That(
            mentionsOpenOrBrowse || mentionsScenarioJson,
            Is.True,
            $"Expected NoSessionMessage to mention Open/Browse or scenario.json; got: {message}");
        // Spec requires actionable guidance: both Open/Browse path and scenario.json.
        Assert.That(lower, Does.Contain("open").Or.Contain("browse"));
        Assert.That(lower, Does.Contain("scenario.json"));
    }

    [Test]
    public void DefaultScenarioRelativeCandidates_is_non_empty_and_prefers_ui_dev_scenario()
    {
        var candidates = ScenarioMapAuthoringHostPolicy.DefaultScenarioRelativeCandidates;
        Assert.That(candidates, Is.Not.Null.And.Not.Empty);
        Assert.That(candidates[0], Does.Contain("russia-vs-nato-baltic-ui-dev"));
    }

    [Test]
    public void ResolveDefaultScenarioPath_returns_null_for_null_empty_or_missing_dir()
    {
        Assert.That(ScenarioMapAuthoringHostPolicy.ResolveDefaultScenarioPath(null), Is.Null);
        Assert.That(ScenarioMapAuthoringHostPolicy.ResolveDefaultScenarioPath(""), Is.Null);
        Assert.That(ScenarioMapAuthoringHostPolicy.ResolveDefaultScenarioPath("   "), Is.Null);
        Assert.That(
            ScenarioMapAuthoringHostPolicy.ResolveDefaultScenarioPath(
                Path.Combine(Path.GetTempPath(), "no-such-repo-root-" + Guid.NewGuid().ToString("N"))),
            Is.Null);
    }

    [Test]
    public void ResolveDefaultScenarioPath_from_repo_root_resolves_ui_dev_scenario_when_present()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null.And.Not.Empty, "Could not locate ProjectAegis.sln for repo root");

        var expectedRel = Path.Combine(
            "assets", "data", "scenarios", "authoring", "russia-vs-nato-baltic-ui-dev.json");
        var expectedFull = Path.GetFullPath(Path.Combine(repoRoot!, expectedRel));
        Assert.That(File.Exists(expectedFull), Is.True, $"Expected UI-dev scenario at {expectedFull}");

        var resolved = ScenarioMapAuthoringHostPolicy.ResolveDefaultScenarioPath(repoRoot);
        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved, Does.EndWith("russia-vs-nato-baltic-ui-dev.json"));
        Assert.That(Path.GetFullPath(resolved!), Is.EqualTo(expectedFull));
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}
