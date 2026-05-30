namespace ProjectAegis.Sim.Policy;

/// <summary>Evaluates policy for an action (ADR-002). Replaces IRoeFilter at orchestrator boundary.</summary>
public interface IPolicyEvaluator
{
    PolicyVerdict Evaluate(in PolicyContext ctx, in ActionRequest request);
}
