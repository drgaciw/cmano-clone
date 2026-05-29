namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Targets;

public sealed record SimEntityBinding(
    EntityKey Entity,
    TargetId TargetId,
    ICommandableTarget Target);
