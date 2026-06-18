namespace ProjectAegis.Delegation.Projection;

/// <summary>Headless-testable atlas load contract for APP-6 USS frame sprites.</summary>
public interface IApp6AtlasAvailability
{
    bool IsLoaded { get; }

    bool HasFrame(string ussFrameId);
}