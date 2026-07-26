namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// Clears <c>AEGIS_PUBLIC_CORPUS</c> for the CatalogSqlite collection so Baltic-seed
/// and default sensor-platform expectations are not polluted by enterprise corpus mode.
/// </summary>
public sealed class CatalogSqliteEnvFixture : IDisposable
{
    public const string PublicCorpusEnvName = "AEGIS_PUBLIC_CORPUS";

    private readonly string? _previous;

    public CatalogSqliteEnvFixture()
    {
        _previous = Environment.GetEnvironmentVariable(PublicCorpusEnvName);
        Environment.SetEnvironmentVariable(PublicCorpusEnvName, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(PublicCorpusEnvName, _previous);
    }
}
