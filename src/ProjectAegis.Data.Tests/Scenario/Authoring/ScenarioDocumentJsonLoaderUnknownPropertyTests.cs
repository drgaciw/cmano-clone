using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario.Authoring;

public sealed class ScenarioDocumentJsonLoaderUnknownPropertyTests
{
    [Theory]
    [InlineData("""{ "unknown": 1, "unknown": 2 }""")]
    [InlineData("""{ "metadata": { "unknown": 1, "unknown": 2 } }""")]
    [InlineData("""{ "unknown": { "value": 1, "value": 2 } }""")]
    [InlineData("""{ "unknown": [{ "value": 1, "value": 2 }] }""")]
    [InlineData("""{ "unknown": 1, "\u0075nknown": 2 }""")]
    public void Strict_load_rejects_duplicates_in_unmapped_content(string json)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json);
            var ex = Assert.Throws<InvalidDataException>(() =>
                ScenarioDocumentJsonLoader.LoadFromFile(path, strict: true));
            Assert.IsType<System.Text.Json.JsonException>(ex.InnerException);
            Assert.NotNull(ScenarioDocumentJsonLoader.LoadFromFile(path));
            Assert.NotNull(ScenarioDocumentJsonLoader.LoadFromFile(path, strict: false));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Both_tolerant_entry_points_keep_last_case_insensitive_member_value()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{ "metadata": { "title": "first", "Title": "second" } }""");
            Assert.Equal("second", ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.Title);
            Assert.Equal("second", ScenarioDocumentJsonLoader.LoadFromFile(path, strict: false).Metadata.Title);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
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
            Assert.Equal("first", document.Variables!["A"]);
            Assert.Equal("second", document.Variables["a"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
