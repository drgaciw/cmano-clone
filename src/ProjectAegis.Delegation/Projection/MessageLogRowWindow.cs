namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Selects which projected lines a row-capped HUD message log shows (UAT D38).
/// A plain tail window lets chatty categories (agent decisions, policy denials) push rare comms
/// state transitions out of the panel; those are pinned so the operator always sees them.
/// Read-only: never mutates the log or projection.
/// </summary>
public static class MessageLogRowWindow
{
    /// <summary>Category of comms state transition lines that must not be evicted by the row cap.</summary>
    public const string CommsCategory = "COMMS";

    /// <summary>
    /// Returns at most <paramref name="maxRows"/> lines in original (chronological) order: every
    /// <c>COMMS</c> line (newest first if they alone exceed the cap) plus the newest remaining lines.
    /// </summary>
    /// <param name="lines">Projected lines, chronological.</param>
    /// <param name="maxRows">Row cap; values &lt;= 0 mean unlimited.</param>
    /// <returns>The lines to display.</returns>
    /// <exception cref="ArgumentNullException">When lines is null.</exception>
    public static IReadOnlyList<MessageLogLine> Select(IReadOnlyList<MessageLogLine> lines, int maxRows)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        if (maxRows <= 0 || lines.Count <= maxRows)
        {
            return lines;
        }

        var keep = new bool[lines.Count];
        var budget = maxRows;
        for (var i = lines.Count - 1; i >= 0 && budget > 0; i--)
        {
            if (string.Equals(lines[i].Category, CommsCategory, StringComparison.Ordinal))
            {
                keep[i] = true;
                budget--;
            }
        }

        for (var i = lines.Count - 1; i >= 0 && budget > 0; i--)
        {
            if (!keep[i])
            {
                keep[i] = true;
                budget--;
            }
        }

        var result = new List<MessageLogLine>(maxRows);
        for (var i = 0; i < lines.Count; i++)
        {
            if (keep[i])
            {
                result.Add(lines[i]);
            }
        }

        return result;
    }
}
