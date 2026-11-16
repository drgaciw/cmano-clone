namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>In-memory idempotency gate for Propose-lane acceptance (no double apply).</summary>
public sealed class AuthoringIdempotencyStore
{
    private readonly HashSet<string> _accepted = new(StringComparer.Ordinal);

    /// <summary>Seeds keys already accepted (fixture priorAcceptedIdempotencyKeys).</summary>
    public void Seed(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _accepted.Add(key);
            }
        }
    }

    /// <summary>Returns true if the key was already accepted.</summary>
    public bool Contains(string idempotencyKey) =>
        !string.IsNullOrWhiteSpace(idempotencyKey) && _accepted.Contains(idempotencyKey);

    /// <summary>Records a newly accepted proposal key.</summary>
    public void Accept(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        _accepted.Add(idempotencyKey);
    }
}
