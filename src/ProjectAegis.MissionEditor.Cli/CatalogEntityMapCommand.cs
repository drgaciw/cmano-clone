namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Catalog;

public static class CatalogEntityMapCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(TextWriter output)
    {
        var rows = CatalogEntityMap.All
            .OrderBy(b => b.EntityName, StringComparer.Ordinal)
            .Select(b => new
        {
            entity = b.EntityName,
            table = b.TableName,
            primaryKey = b.PrimaryKeyColumns,
            orderBy = b.DeterministicOrderBy,
            dto = b.RuntimeDto,
        });
        output.WriteLine(JsonSerializer.Serialize(new { ok = true, entities = rows }, JsonOptions));
        return 0;
    }
}