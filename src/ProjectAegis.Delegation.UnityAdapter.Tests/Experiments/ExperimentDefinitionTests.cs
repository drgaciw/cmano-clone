using System.Globalization;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Experiments;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Experiments;

public sealed class ExperimentDefinitionTests
{
    [Test]
    public void Json_roundtrip_preserves_order_duplicates_and_opaque_metadata()
    {
        var original = new ExperimentDefinition("baltic-patrol", [8, -7, 8, 0], 2,
            "doctrine,\"quoted\"\nlabel", "roe α");
        var json = ExperimentDefinitionJson.Serialize(original);
        var restored = ExperimentDefinitionJson.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(restored.ScenarioId, Is.EqualTo(original.ScenarioId));
            Assert.That(restored.Seeds, Is.EqualTo(original.Seeds));
            Assert.That(restored.Ticks, Is.EqualTo(2));
            Assert.That(restored.DoctrineId, Is.EqualTo(original.DoctrineId));
            Assert.That(restored.RoeId, Is.EqualTo(original.RoeId));
            Assert.That(ExperimentDefinitionJson.Serialize(restored), Is.EqualTo(json));
        });
    }

    [TestCase("{", "JSON")]
    [TestCase("null", "object")]
    [TestCase("[]", "object")]
    [TestCase("{}", "scenarioId")]
    [TestCase("{\"scenarioId\":\"\",\"seeds\":[1],\"ticks\":1}", "scenarioId")]
    [TestCase("{\"scenarioId\":null,\"seeds\":[1],\"ticks\":1}", "scenarioId")]
    [TestCase("{\"scenarioId\":1,\"seeds\":[1],\"ticks\":1}", "scenarioId")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"ticks\":1}", "seeds")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[],\"ticks\":1}", "seeds")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":null,\"ticks\":1}", "seeds")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1.5],\"ticks\":1}", "seeds[0]")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[2147483648],\"ticks\":1}", "seeds[0]")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[\"1\"],\"ticks\":1}", "seeds[0]")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1]}", "ticks")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1],\"ticks\":0}", "ticks")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1],\"ticks\":-1}", "ticks")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1],\"ticks\":2.5}", "ticks")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1],\"ticks\":2147483648}", "ticks")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1],\"ticks\":1,\"doctrineId\":\" \"}", "doctrineId")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1],\"ticks\":1,\"roeId\":null}", "roeId")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1],\"ticks\":1,\"roeId\":2}", "roeId")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1],\"ticks\":1,\"tick\":2}", "tick")]
    [TestCase("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[1],\"ticks\":1,\"ticks\":2}", "ticks")]
    public void Invalid_json_reports_the_field(string json, string expected)
    {
        Assert.That(() => ExperimentDefinitionJson.Parse(json),
            Throws.TypeOf<FormatException>().With.Message.Contains(expected));
    }

    [Test]
    public void Definition_copies_the_input_seed_grid()
    {
        var seeds = new[] { 8, 7 };
        var definition = new ExperimentDefinition("baltic-patrol", seeds, 1);
        seeds[0] = 99;
        Assert.That(definition.Seeds, Is.EqualTo(new[] { 8, 7 }));
        Assert.That(() => ((IList<int>)definition.Seeds)[0] = 99, Throws.TypeOf<NotSupportedException>());
    }

    [Test]
    public void Constructor_rejects_invalid_direct_inputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new ExperimentDefinition(" ", [1], 1), Throws.ArgumentException);
            Assert.That(() => new ExperimentDefinition("baltic-patrol", [], 1), Throws.ArgumentException);
            Assert.That(() => new ExperimentDefinition("baltic-patrol", [1], 0), Throws.ArgumentException);
            Assert.That(() => new ExperimentDefinition("baltic-patrol", [1], 1, roeId: " "), Throws.ArgumentException);
        });
    }

    [Test]
    public void Real_runs_preserve_seed_order_row_count_and_repeated_fingerprints()
    {
        var definition = ExperimentDefinitionJson.Parse("{\"scenarioId\":\"baltic-patrol\",\"seeds\":[8,7,8],\"ticks\":2}");
        var first = SeedGridExperimentRunner.Run(definition);
        var second = SeedGridExperimentRunner.Run(definition);
        Assert.Multiple(() =>
        {
            Assert.That(first.Select(row => row.Seed), Is.EqualTo(new[] { 8, 7, 8 }));
            Assert.That(first, Has.Count.EqualTo(3));
            Assert.That(first.Select(row => row.Result.Fingerprint), Is.EqualTo(second.Select(row => row.Result.Fingerprint)));
            Assert.That(first[0].Result.Fingerprint, Is.EqualTo(first[2].Result.Fingerprint).And.Not.Empty);
            Assert.That(first.All(row => row.Result.Ticks == 2), Is.True);
            Assert.That(SeedGridExperimentRunner.ExportCsv(first), Is.EqualTo(SeedGridExperimentRunner.ExportCsv(second)));
            Assert.That(SeedGridExperimentRunner.ExportCsv(first).Split('\n', StringSplitOptions.RemoveEmptyEntries), Has.Length.EqualTo(4));
        });
    }

    [Test]
    public void Opaque_metadata_with_csv_special_characters_does_not_override_policy_or_change_csv()
    {
        var normal = SeedGridExperimentRunner.Run(new ExperimentDefinition("baltic-patrol", [7], 1));
        var tagged = SeedGridExperimentRunner.Run(new ExperimentDefinition("baltic-patrol", [7], 1, "a,\"b\"\nc", "roe,id"));
        Assert.That(SeedGridExperimentRunner.ExportCsv(tagged), Is.EqualTo(SeedGridExperimentRunner.ExportCsv(normal)));
    }

    [Test]
    [NonParallelizable]
    public void Real_runner_csv_is_identical_across_cultures()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            var definition = new ExperimentDefinition("baltic-patrol", [-7, 8], 1);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var english = SeedGridExperimentRunner.ExportCsv(SeedGridExperimentRunner.Run(definition));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.That(SeedGridExperimentRunner.ExportCsv(SeedGridExperimentRunner.Run(definition)), Is.EqualTo(english));
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Test]
    public void Unknown_scenario_fails_instead_of_falling_back()
    {
        Assert.That(() => SeedGridExperimentRunner.Run(new ExperimentDefinition("no-such-scenario", [1], 1)),
            Throws.InvalidOperationException.With.Message.Contains("no-such-scenario"));
    }
}
