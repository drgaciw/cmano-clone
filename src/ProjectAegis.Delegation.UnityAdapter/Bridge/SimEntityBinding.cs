namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using Core;
using Targets;

public sealed record SimEntityBinding(
    EntityKey Entity,
    TargetId TargetId,
    ICommandableTarget Target);
