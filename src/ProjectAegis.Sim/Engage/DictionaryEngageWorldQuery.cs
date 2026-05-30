namespace ProjectAegis.Sim.Engage;

public sealed class DictionaryEngageWorldQuery : IEngageWorldQuery
{
    private readonly Dictionary<(ulong Shooter, ulong Target, ulong Mount), EngageContext> _contexts = new();

    public void Set(in EngageRequest request, in EngageContext context) =>
        _contexts[(request.ShooterUnitId, request.TargetId, request.MountId)] = context;

    public bool TryGetContext(in EngageRequest request, out EngageContext context) =>
        _contexts.TryGetValue((request.ShooterUnitId, request.TargetId, request.MountId), out context);
}
