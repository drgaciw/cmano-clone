namespace ProjectAegis.Delegation.Hindsight;

using ProjectAegis.Delegation.Decision;

public interface IHindsightOrderLogHook
{
    void RegisterAgent(Core.AgentId agentId, string? personalitySlug);

    void OnAppended(OrderLogEntry entry);
}
