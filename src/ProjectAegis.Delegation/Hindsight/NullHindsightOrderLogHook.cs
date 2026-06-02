namespace ProjectAegis.Delegation.Hindsight;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;

public sealed class NullHindsightOrderLogHook : IHindsightOrderLogHook
{
    public static NullHindsightOrderLogHook Instance { get; } = new();

    public void RegisterAgent(AgentId agentId, string? personalitySlug)
    {
    }

    public void OnAppended(OrderLogEntry entry)
    {
    }
}
