namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Reflection;
using Cli;
using Data.Catalog;
using Data.Scenario.Authoring;
using NUnit.Framework;

/// <summary>
/// Proves <see cref="ScenarioValidateCommand.ResolveCatalogPublic"/> opens
/// <c>aegis_public_corpus.db</c> for a public-corpus <c>dbRef</c> and keeps
/// Baltic / default scenarios on <c>baltic_patrol.db</c>.
/// </summary>
[TestFixture]
public sealed class ScenarioValidateResolveCatalogTests
{
    [Test]
    public void ResolveCatalog_public_corpus_dbRef_opens_corpus_catalog_and_matches_tl0_train()
    {
        using var reader = Open(Document("aegis_public_corpus"));

        Assert.That(DatabasePath(reader), Does.EndWith(CatalogValidationDefaults.PublicCorpusDatabaseFileName));
        Assert.That(reader.TryResolveDbRef("aegis_public_corpus", out var snapshot), Is.True);
        Assert.That(snapshot, Is.EqualTo(CatalogValidationDefaults.PublicCorpusSnapshotId));
        Assert.That(reader.TryResolveSnapshotForTlBranch("TL-0", out var train, out _), Is.True);
        Assert.That(train, Is.EqualTo(snapshot));
        Assert.That(reader.TryGetCombatRadiusNm("type-082ii-wozang-818-kunshan-2012", out _), Is.True);
    }

    [Test]
    public void ResolveCatalog_public_corpus_alias_opens_corpus_catalog()
    {
        using var reader = Open(Document("public-corpus"));

        Assert.That(DatabasePath(reader), Does.EndWith(CatalogValidationDefaults.PublicCorpusDatabaseFileName));
        Assert.That(reader.TryResolveDbRef("public-corpus", out var snapshot), Is.True);
        Assert.That(snapshot, Is.EqualTo(CatalogValidationDefaults.PublicCorpusSnapshotId));
    }

    [Test]
    public void ResolveCatalog_baltic_dbRef_keeps_baltic_catalog()
    {
        using var reader = Open(Document("baltic_patrol"));

        Assert.That(DatabasePath(reader), Does.EndWith("baltic_patrol.db"));
        Assert.That(reader.TryResolveSnapshotForTlBranch("TL-0", out var train, out _), Is.True);
        Assert.That(train, Is.EqualTo(CatalogValidationDefaults.BalticSnapshotId));
        Assert.That(reader.TryGetCombatRadiusNm("type-082ii-wozang-818-kunshan-2012", out _), Is.False);
    }

    [Test]
    public void ResolveCatalog_without_dbRef_keeps_baltic_catalog()
    {
        using var reader = Open(Document(dbRef: null));

        Assert.That(DatabasePath(reader), Does.EndWith("baltic_patrol.db"));
        Assert.That(reader.TryResolveSnapshotForTlBranch("TL-0", out var train, out _), Is.True);
        Assert.That(train, Is.EqualTo(CatalogValidationDefaults.BalticSnapshotId));
    }

    [Test]
    public void scenario_validate_enterprise_public_corpus_smoke_exits_0()
    {
        var path = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine(
                "assets",
                "data",
                "scenarios",
                "smoke",
                "enterprise-public-corpus-smoke.scenario.json"));
        Assert.That(File.Exists(path), Is.True, path);

        using var writer = new StringWriter();
        var exit = ScenarioValidateCommand.Run(path, quiet: false, writer);
        Assert.That(exit, Is.EqualTo(0), writer.ToString());
        Assert.That(writer.ToString(), Does.Not.Contain("TL_RELEASE_TRAIN_MISMATCH"));
    }

    private static ScenarioDocumentDto Document(string? dbRef) =>
        new()
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = dbRef,
                TlBranch = "TL-0",
            },
        };

    private static SqliteCatalogReader Open(ScenarioDocumentDto scenario)
    {
        var method = typeof(ScenarioValidateCommand).GetMethod(
            "ResolveCatalogPublic",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        var reader = method!.Invoke(null, [scenario]);
        Assert.That(reader, Is.InstanceOf<SqliteCatalogReader>(), reader?.GetType().FullName);
        return (SqliteCatalogReader)reader!;
    }

    private static string DatabasePath(SqliteCatalogReader reader)
    {
        var field = typeof(SqliteCatalogReader).GetField(
            "_databasePath",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (string)field!.GetValue(reader)!;
    }
}
