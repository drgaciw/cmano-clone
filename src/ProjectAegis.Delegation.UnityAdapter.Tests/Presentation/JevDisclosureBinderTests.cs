using System.Text.Json;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>DRG-319: skill-unavailable disclosure chrome. No engage path.</summary>
[TestFixture]
public sealed class JevDisclosureBinderTests
{
    [Test]
    public void Skill_down_shows_disclosure_and_does_not_invent_a_model_score()
    {
        var panel = JevAttentionBinder.Project(Read("skill_unavailable.json"));

        Assert.That(panel.Disclosure.Visible, Is.True);
        Assert.That(panel.Disclosure.CssClass, Is.EqualTo(JevDisclosurePresentation.VisibleCssClass));
        Assert.That(panel.Disclosure.BannerText, Does.Contain("ASSIST UNAVAILABLE"));
        Assert.That(panel.Disclosure.BannerText, Does.Not.Contain("WEAPON").And.Not.Contain("ENGAGE"));
        Assert.That(panel.ModelScoreAvailable, Is.False);
        Assert.That(panel.ScoreSourceLabel, Is.EqualTo("SOURCE: UNAVAILABLE"));
        Assert.That(panel.Rows[0].Level, Is.Null);
        Assert.That(panel.Rows[0].Highlight, Is.False);
        Assert.That(panel.Disclosure.ModelPin, Is.EqualTo(JevDisclosurePresentation.PinnedModel));
        Assert.That(panel.Disclosure.OpenRouterModel, Is.EqualTo(JevDisclosurePresentation.PinnedOpenRouterModel));
    }

    [Test]
    public void Skill_available_labels_local_heuristics_and_stays_advisory()
    {
        var panel = JevAttentionBinder.Project(Read("skill_available.json"));

        Assert.That(panel.Disclosure.Visible, Is.True);
        Assert.That(panel.Disclosure.CssClass, Is.EqualTo(JevDisclosurePresentation.VisibleCssClass));
        Assert.That(panel.Disclosure.BannerText, Does.Contain("LOCAL HEURISTIC"));
        Assert.That(panel.ModelScoreAvailable, Is.False);
        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Watch));
        Assert.That(panel.Rows[0].Line, Does.Not.Contain("order").IgnoreCase);
    }

    [Test]
    public void Missing_skill_flag_fails_closed_onto_disclosure()
    {
        Assert.That(JevDisclosureBinder.Project(null).Visible, Is.True);
        Assert.That(JevDisclosureBinder.Project(false).Visible, Is.True);
        Assert.That(JevDisclosureBinder.Project(true).Visible, Is.False);
    }

    [Test]
    public void Question_payload_pins_jev_1_13_and_forbids_enqueue()
    {
        using var document = JsonDocument.Parse(Read("questions_vs1.json"));
        var root = document.RootElement;
        var questions = root.GetProperty("questions");
        var attention = questions.GetProperty("attention_priority").GetProperty("instructions").GetString();
        var disclosure = questions.GetProperty("skill_unavailable_disclosure").GetProperty("instructions").GetString();

        Assert.That(root.GetProperty("pinModel").GetString(), Is.EqualTo(JevDisclosurePresentation.PinnedModel));
        Assert.That(root.GetProperty("openRouterModel").GetString(), Is.EqualTo(JevDisclosurePresentation.PinnedOpenRouterModel));
        Assert.That(root.GetProperty("pinModel").GetString(), Does.Not.Contain("latest"));
        Assert.That(root.GetProperty("openRouterModel").GetString(), Does.Not.Contain("latest"));
        Assert.That(attention, Does.Contain("Never imply engage"));
        Assert.That(disclosure, Does.Contain("disclosure only"));
    }

    private static string Read(string name)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return File.ReadAllText(Path.Combine(
                    dir,
                    "src",
                    "ProjectAegis.Delegation.UnityAdapter.Tests",
                    "Fixtures",
                    "Jev",
                    "vs1",
                    name));
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
