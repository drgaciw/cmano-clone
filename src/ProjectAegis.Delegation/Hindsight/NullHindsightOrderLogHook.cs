namespace ProjectAegis.Delegation.Hindsight;

using Core;
using Decision;

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
