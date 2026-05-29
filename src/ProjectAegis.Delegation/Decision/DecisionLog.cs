namespace ProjectAegis.Delegation.Decision;

public sealed class DecisionLog
{
    private readonly List<DecisionRecord> _records = new();

    public IReadOnlyList<DecisionRecord> Records => _records;

    public void Append(DecisionRecord record) => _records.Add(record);
}
