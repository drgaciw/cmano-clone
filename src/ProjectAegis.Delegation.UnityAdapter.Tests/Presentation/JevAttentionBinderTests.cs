using System.Text.Json;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>DRG-319: attention Score sort and highlight from synthetic VS1 fixtures.</summary>
[TestFixture]
public sealed class JevAttentionBinderTests
{
    [Test]
    public void Immediate_fixture_is_the_top_highlight()
    {
        var panel = JevAttentionBinder.Project(Read("attention_immediate.json"));

        Assert.That(panel.FixtureId, Is.EqualTo("vs1-attention-immediate"));
        Assert.That(panel.ModelScoreAvailable, Is.False);
        Assert.That(panel.Disclosure.Visible, Is.False);
        Assert.That(panel.Disclosure.OpenRouterModel, Is.EqualTo(JevDisclosurePresentation.PinnedOpenRouterModel));
        Assert.That(panel.Rows, Has.Count.EqualTo(1));
        Assert.That(panel.Rows[0].UnitId, Is.EqualTo("u-fixture-01"));
        Assert.That(panel.Rows[0].ContactId, Is.EqualTo("c-fixture-hostile-closing"));
        Assert.That(panel.Rows[0].Line, Does.Contain("c-fixture-hostile-closing"));
        Assert.That(panel.AttributionLine, Does.Contain("HEURISTIC").And.Contain(panel.FixtureId));
        Assert.That(panel.AttributionLine, Does.Not.Contain(JevDisclosurePresentation.PinnedOpenRouterModel));
        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Immediate));
        Assert.That(panel.Rows[0].Highlight, Is.True);
        Assert.That(panel.Rows[0].CssClass, Is.EqualTo(JevAttentionLevel.CssClass(JevAttentionLevel.Immediate)));
        Assert.That(panel.Rows[0].Line, Does.Not.Contain("ENGAGE").And.Not.Contain("WEAPON").And.Not.Contain("SALVO"));
    }

    [Test]
    public void Ignore_fixture_has_no_highlight()
    {
        var panel = JevAttentionBinder.Project(Read("attention_ignore.json"));

        Assert.That(panel.Rows[0].UnitId, Is.EqualTo("u-fixture-02"));
        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Ignore));
        Assert.That(panel.Rows[0].Highlight, Is.False);
        Assert.That(panel.Disclosure.Visible, Is.False);
    }

    [Test]
    public void Mixed_roster_sorts_immediate_through_ignore()
    {
        var json = Read("attention_mixed_roster.json");
        var panel = JevAttentionBinder.Project(json);
        using var document = JsonDocument.Parse(json);
        var expectedIds = document.RootElement.GetProperty("expectedSortOrderHint").EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        var expectedLevels = document.RootElement.GetProperty("roster").EnumerateArray()
            .ToDictionary(
                row => row.GetProperty("unitId").GetString()!,
                row => row.GetProperty("expectedAttentionLevel").GetString());

        Assert.That(panel.Rows.Select(row => row.UnitId).ToArray(), Is.EqualTo(expectedIds));
        foreach (var row in panel.Rows)
        {
            Assert.That(row.Level, Is.EqualTo(expectedLevels[row.UnitId]));
        }

        Assert.That(panel.Disclosure.Visible, Is.False);
        Assert.That(panel.Rows[0].Highlight, Is.True);
        Assert.That(panel.Rows[^1].Highlight, Is.False);
    }

    [Test]
    public void Sort_is_rank_then_unit_id_not_input_order()
    {
        var rows = new[]
        {
            Row("u-b", JevAttentionLevel.Ignore),
            Row("u-a", JevAttentionLevel.Immediate),
            Row("u-c", JevAttentionLevel.Immediate),
        };

        var sorted = JevAttentionBinder.Sort(rows);

        Assert.That(sorted.Select(row => row.UnitId).ToArray(), Is.EqualTo(new[] { "u-a", "u-c", "u-b" }));
    }

    [Test]
    public void Oracle_fields_are_not_required_to_score()
    {
        var stripped = StripExpected(Read("attention_immediate.json"));
        var panel = JevAttentionBinder.Project(stripped);

        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Immediate));
    }

    [Test]
    public void Private_log_payloads_are_refused()
    {
        const string json = """
            {"schema":"aegis.jev.state.v0","unitId":"u-fixture-x","decisionLog":[]}
            """;

        var error = Assert.Throws<InvalidOperationException>(() => JevAttentionBinder.Project(json));
        Assert.That(error!.Message, Does.Contain("projection fixtures"));
    }

    [Test]
    public void Questions_document_is_not_a_state_projection()
    {
        Assert.Throws<InvalidOperationException>(() => JevAttentionBinder.Project(Read("questions_vs1.json")));
    }

    [Test]
    public void Missing_range_does_not_flag_a_closing_hostile_as_immediate()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "fixtureId": "vs1-missing-range",
              "unitId": "u-close",
              "contactId": "c-close",
              "skillAvailability": { "jevDecide": true },
              "playerInfoModel": { "selectedUnitId": "u-other", "cameraFocusId": null },
              "contact": { "iff": "hostile", "closing": true }
            }
            """);

        Assert.That(panel.ModelScoreAvailable, Is.False);
        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Low));
        Assert.That(panel.Rows[0].ContactId, Is.EqualTo("c-close"));
    }

    [Test]
    public void Missing_range_does_not_flag_a_closing_unknown_as_focus()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "unitId": "u-unknown",
              "skillAvailability": { "jevDecide": true },
              "playerInfoModel": { "selectedUnitId": "u-other" },
              "contact": { "iff": "unknown", "closing": true }
            }
            """);

        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Low));
    }

    [Test]
    public void Non_numeric_range_is_unknown_not_zero()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "unitId": "u-text-range",
              "skillAvailability": { "jevDecide": true },
              "playerInfoModel": { "selectedUnitId": "u-other" },
              "contact": { "iff": "hostile", "closing": true, "rangeNm": "unknown" }
            }
            """);

        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Low));
    }

    [Test]
    public void Explicit_zero_range_closing_hostile_is_immediate()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "unitId": "u-zero",
              "skillAvailability": { "jevDecide": true },
              "playerInfoModel": { "selectedUnitId": "u-other" },
              "contact": { "iff": "hostile", "closing": true, "rangeNm": 0 }
            }
            """);

        Assert.That(panel.ModelScoreAvailable, Is.False);
        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Immediate));
    }

    [Test]
    public void Already_looking_scores_below_an_identical_unattended_contact()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "fixtureId": "vs1-already-looking",
              "skillAvailability": { "jevDecide": true },
              "playerInfoModel": { "selectedUnitId": "u-attended", "cameraFocusId": null },
              "roster": [
                {
                  "unitId": "u-attended",
                  "contactId": "c-attended",
                  "contact": { "iff": "hostile", "rangeNm": 6, "closing": true },
                  "flags": { "urgencyHint": "critical" }
                },
                {
                  "unitId": "u-unattended",
                  "contactId": "c-open",
                  "contact": { "iff": "hostile", "rangeNm": 6, "closing": true },
                  "flags": { "urgencyHint": "critical" }
                }
              ]
            }
            """);

        var attended = panel.Rows.Single(row => row.UnitId == "u-attended");
        var unattended = panel.Rows.Single(row => row.UnitId == "u-unattended");
        Assert.That(unattended.Level, Is.EqualTo(JevAttentionLevel.Immediate));
        Assert.That(attended.Level, Is.EqualTo(JevAttentionLevel.Focus));
        Assert.That(attended.ContactId, Is.EqualTo("c-attended"));
        Assert.That(panel.Rows[0].UnitId, Is.EqualTo("u-unattended"));
        Assert.That(panel.ModelScoreAvailable, Is.False);
    }

    [Test]
    public void Camera_focus_counts_as_already_looking()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "skillAvailability": { "jevDecide": true },
              "playerInfoModel": { "selectedUnitId": "u-other", "cameraFocusId": "u-framed" },
              "roster": [
                {
                  "unitId": "u-framed",
                  "contact": { "iff": "unknown", "rangeNm": 14, "closing": true },
                  "flags": { "urgencyHint": "urgent" }
                },
                {
                  "unitId": "u-open",
                  "contact": { "iff": "unknown", "rangeNm": 14, "closing": true },
                  "flags": { "urgencyHint": "urgent" }
                }
              ]
            }
            """);

        Assert.That(panel.Rows.Single(row => row.UnitId == "u-open").Level, Is.EqualTo(JevAttentionLevel.Focus));
        Assert.That(panel.Rows.Single(row => row.UnitId == "u-framed").Level, Is.EqualTo(JevAttentionLevel.Watch));
    }

    [Test]
    public void Pinned_jev_decision_marks_model_score_and_uses_that_level()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "fixtureId": "vs1-model-result",
              "unitId": "u-model",
              "contactId": "c-model",
              "skillAvailability": { "jevDecide": true },
              "playerInfoModel": { "selectedUnitId": "u-other" },
              "contact": { "iff": "hostile", "rangeNm": 4, "closing": true },
              "jevDecision": { "model": "typesafe/jev-1.13", "attention_priority": "watch" }
            }
            """);

        Assert.That(panel.ModelScoreAvailable, Is.True);
        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Watch));
        Assert.That(panel.Disclosure.OpenRouterModel, Is.EqualTo("typesafe/jev-1.13"));
        Assert.That(panel.FixtureId, Is.EqualTo("vs1-model-result"));
        Assert.That(panel.AttributionLine, Does.Contain("MODEL:").And.Contain(panel.Disclosure.OpenRouterModel).And.Contain(panel.FixtureId));
    }

    [Test]
    public void Decision_without_the_pinned_model_stays_a_heuristic()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "unitId": "u-other-model",
              "skillAvailability": { "jevDecide": true },
              "playerInfoModel": { "selectedUnitId": "u-other" },
              "contact": { "iff": "hostile", "rangeNm": 4, "closing": true },
              "jevDecision": { "model": "other/model", "attention_priority": "watch" }
            }
            """);

        Assert.That(panel.ModelScoreAvailable, Is.False);
        Assert.That(panel.Rows[0].Level, Is.EqualTo(JevAttentionLevel.Immediate));
    }

    [Test]
    public void Partial_roster_result_does_not_mark_the_panel_as_a_model_score()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "skillAvailability": { "jevDecide": true },
              "playerInfoModel": { "selectedUnitId": "u-other" },
              "roster": [
                {
                  "unitId": "u-model",
                  "contact": { "iff": "hostile", "rangeNm": 4, "closing": true },
                  "jevDecision": { "model": "typesafe/jev-1.13", "attention_priority": "watch" }
                },
                {
                  "unitId": "u-local",
                  "contact": { "iff": "hostile", "rangeNm": 4, "closing": true }
                }
              ]
            }
            """);

        Assert.That(panel.ModelScoreAvailable, Is.False);
        Assert.That(panel.Rows.Single(row => row.UnitId == "u-model").Level, Is.EqualTo(JevAttentionLevel.Watch));
        Assert.That(panel.Rows.Single(row => row.UnitId == "u-local").Level, Is.EqualTo(JevAttentionLevel.Immediate));
    }

    [Test]
    public void Skill_down_ignores_a_stale_jev_decision()
    {
        var panel = JevAttentionBinder.Project("""
            {
              "schema": "aegis.jev.state.v0",
              "unitId": "u-stale",
              "skillAvailability": { "jevDecide": false },
              "contact": { "iff": "hostile", "rangeNm": 4, "closing": true },
              "jevDecision": { "model": "typesafe/jev-1.13", "attention_priority": "immediate" }
            }
            """);

        Assert.That(panel.Disclosure.Visible, Is.True);
        Assert.That(panel.ModelScoreAvailable, Is.False);
        Assert.That(panel.Rows[0].Level, Is.Null);
    }

    private static JevAttentionRow Row(string unitId, string level) =>
        new(unitId, null, level, JevAttentionLevel.Rank(level), JevAttentionLevel.Highlights(level),
            JevAttentionLevel.CssClass(level));

    private static string StripExpected(string json)
    {
        using var document = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteWithoutExpected(writer, document.RootElement);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteWithoutExpected(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.StartsWith("expected", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    WriteWithoutExpected(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteWithoutExpected(writer, item);
                }

                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static string Read(string name)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(
            root,
            "src",
            "ProjectAegis.Delegation.UnityAdapter.Tests",
            "Fixtures",
            "Jev",
            "vs1",
            name);
        Assert.That(File.Exists(path), Is.True, path);
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
