namespace ProjectAegis.Delegation.Hindsight;

using System.Collections.Concurrent;
using Core;
using Decision;

public sealed class HindsightOrderLogHook : IHindsightOrderLogHook
{
    private readonly IHindsightMemoryClient _client;
    private readonly ConcurrentDictionary<string, string> _personalityByAgent = new(StringComparer.Ordinal);

    public HindsightOrderLogHook(IHindsightMemoryClient client) => _client = client;

    public void RegisterAgent(AgentId agentId, string? personalitySlug) =>
        _personalityByAgent[agentId.Value] = string.IsNullOrWhiteSpace(personalitySlug) ? "custom" : personalitySlug.Trim();

    /// <summary>Last slug passed to <see cref="RegisterAgent"/> for this agent. A later call overwrites it.</summary>
    internal bool TryGetRegisteredPersonality(AgentId agentId, out string? personality) =>
        _personalityByAgent.TryGetValue(agentId.Value, out personality);

    public void OnAppended(OrderLogEntry entry)
    {
        if (entry.Kind != OrderLogEntryKind.AgentDecision)
        {
            return;
        }

        var payload = entry.Payload switch
        {
            AgentDecisionPayload typed => typed,
            DecisionRecord legacy => AgentDecisionPayload.FromDecisionRecord(legacy, legacy.SimTick),
            _ => null,
        };

        if (payload is null)
        {
            return;
        }

        _personalityByAgent.TryGetValue(payload.AgentId.Value, out var personality);
        personality ??= "custom";

        var bankId = HindsightBankIds.AgentDecision(personality, payload.AgentId.Value);
        var content = AgentDecisionMemoryFormatter.Format(payload, personality);
        _client.RetainFireAndForget(bankId, content, context: "agent-decision");
    }
}
