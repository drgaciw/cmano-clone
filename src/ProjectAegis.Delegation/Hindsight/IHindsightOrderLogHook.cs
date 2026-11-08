namespace ProjectAegis.Delegation.Hindsight;

using Decision;

public interface IHindsightOrderLogHook
{
    void RegisterAgent(Core.AgentId agentId, string? personalitySlug);

    void OnAppended(OrderLogEntry entry);
}
