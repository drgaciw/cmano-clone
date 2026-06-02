namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Engage;

/// <summary>Scenario-level defaults for MVP engage priming (data/scenarios/*.policy.json).</summary>
public sealed class ScenarioEngageDefaults
{
    public ScenarioEngageDefaults(
        double rangeMeters,
        double envelopeMinMeters,
        double envelopeMaxMeters,
        int defaultMagazineRounds,
        bool hasFireControlTrack,
        double pkBase = 0.85,
        double pkKill = 1.0)
    {
        RangeMeters = rangeMeters;
        EnvelopeMinMeters = envelopeMinMeters;
        EnvelopeMaxMeters = envelopeMaxMeters;
        DefaultMagazineRounds = defaultMagazineRounds;
        HasFireControlTrack = hasFireControlTrack;
        PkBase = pkBase;
        PkKill = pkKill;
    }

    public double RangeMeters { get; }

    public double EnvelopeMinMeters { get; }

    public double EnvelopeMaxMeters { get; }

    public int DefaultMagazineRounds { get; }

    public bool HasFireControlTrack { get; }

    public double PkBase { get; }

    public double PkKill { get; }

    public EngageContext ToEngageContext(int roundsRemaining) =>
        new(
            RangeMeters,
            new WeaponEnvelope(EnvelopeMinMeters, EnvelopeMaxMeters),
            roundsRemaining,
            HasFireControlTrack,
            PkBase: PkBase,
            PkKill: PkKill);

    public static ScenarioEngageDefaults MvpFallback { get; } = new(
        50_000,
        1_000,
        100_000,
        defaultMagazineRounds: 2,
        hasFireControlTrack: true);
}