namespace ProjectAegis.Data.Catalog;

/// <summary>Notifies live <see cref="SqliteCatalogReader"/> instances to drop dependency-graph caches after commit.</summary>
public static class CatalogDependencyGraphCacheInvalidator
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, List<SqliteCatalogReader>> ReadersByPath = new(StringComparer.Ordinal);

    internal static void Register(SqliteCatalogReader reader, string databasePath)
    {
        var normalized = NormalizePath(databasePath);
        lock (Sync)
        {
            if (!ReadersByPath.TryGetValue(normalized, out var readers))
            {
                readers = [];
                ReadersByPath[normalized] = readers;
            }

            if (!readers.Contains(reader))
            {
                readers.Add(reader);
            }
        }
    }

    internal static void Unregister(SqliteCatalogReader reader, string databasePath)
    {
        var normalized = NormalizePath(databasePath);
        lock (Sync)
        {
            if (!ReadersByPath.TryGetValue(normalized, out var readers))
            {
                return;
            }

            readers.Remove(reader);
            if (readers.Count == 0)
            {
                ReadersByPath.Remove(normalized);
            }
        }
    }

    public static void InvalidateForDatabase(string databasePath)
    {
        var normalized = NormalizePath(databasePath);
        SqliteCatalogReader[] readers;
        lock (Sync)
        {
            if (!ReadersByPath.TryGetValue(normalized, out var registered) || registered.Count == 0)
            {
                return;
            }

            readers = registered.ToArray();
        }

        foreach (var reader in readers)
        {
            reader.InvalidateDependencyGraphCache();
        }
    }

    private static string NormalizePath(string databasePath) =>
        Path.GetFullPath(databasePath);
}