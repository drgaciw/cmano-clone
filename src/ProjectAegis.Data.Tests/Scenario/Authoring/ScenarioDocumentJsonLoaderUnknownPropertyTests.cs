using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario.Authoring;

public sealed class ScenarioDocumentJsonLoaderUnknownPropertyTests
{
    [TestCase("""{ "unknown": 1, "unknown": 2 }""")]
    [TestCase("""{ "metadata": { "unknown": 1, "unknown": 2 } }""")]
    [TestCase("""{ "unknown": { "value": 1, "value": 2 } }""")]
    [TestCase("""{ "unknown": [{ "value": 1, "value": 2 }] }""")]
    [TestCase("""{ "unknown": 1, "\u0075nknown": 2 }""")]
    public void Strict_load_rejects_duplicates_in_unmapped_content(string json)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json);
            var ex = Assert.Throws<InvalidDataException>(() =>
                ScenarioDocumentJsonLoader.LoadFromFile(path, strict: true));
            Assert.That(ex!.InnerException, Is.TypeOf<System.Text.Json.JsonException>());
            Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path), Is.Not.Null);
            Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path, strict: false), Is.Not.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Both_tolerant_entry_points_keep_last_case_insensitive_member_value()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{ "metadata": { "title": "first", "Title": "second" } }""");
            Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.Title, Is.EqualTo("second"));
            Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path, strict: false).Metadata.Title, Is.EqualTo("second"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Strict_load_keeps_object_scopes_and_dictionary_key_casing_distinct()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                {
                    "unknown": [{ "value": 1 }, { "value": 2 }],
                    "variables": { "A": "first", "a": "second" }
                }
                """);
            var document = ScenarioDocumentJsonLoader.LoadFromFile(path, strict: true);
            Assert.That(document.Variables!["A"].GetString(), Is.EqualTo("first"));
            Assert.That(document.Variables["a"].GetString(), Is.EqualTo("second"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
