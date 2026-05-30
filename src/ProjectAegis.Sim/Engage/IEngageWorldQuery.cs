namespace ProjectAegis.Sim.Engage;

public interface IEngageWorldQuery
{
    bool TryGetContext(in EngageRequest request, out EngageContext context);
}
