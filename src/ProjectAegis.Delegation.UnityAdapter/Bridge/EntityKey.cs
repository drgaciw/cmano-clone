namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>
/// Opaque sim/ECS entity identifier (e.g. Unity entity index, DOTS entity id).
/// </summary>
public readonly record struct EntityKey(int Value)
{
    public override string ToString() => Value.ToString();
}
