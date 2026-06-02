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
        double pkIntercept = 0.0,
        double pkKill = 1.0,
        int salvoSize = 1)
    {
        RangeMeters = rangeMeters;
        EnvelopeMinMeters = envelopeMinMeters;
        EnvelopeMaxMeters = envelopeMaxMeters;
        DefaultMagazineRounds = defaultMagazineRounds;
        HasFireControlTrack = hasFireControlTrack;
        PkBase = pkBase;
        PkIntercept = pkIntercept;
        PkKill = pkKill;
        SalvoSize = Math.Max(1, salvoSize);
    }

    public double RangeMeters { get; }

    public double EnvelopeMinMeters { get; }

    public double EnvelopeMaxMeters { get; }

    public int DefaultMagazineRounds { get; }

    public bool HasFireControlTrack { get; }

    public double PkBase { get; }

    public double PkIntercept { get; }

    public double PkKill { get; }

    public int SalvoSize { get; }

    public EngageContext ToEngageContext(int roundsRemaining) =>
        new(
            RangeMeters,
            new WeaponEnvelope(EnvelopeMinMeters, EnvelopeMaxMeters),
            roundsRemaining,
            HasFireControlTrack,
            PkBase: PkBase,
            PkIntercept: PkIntercept,
            PkKill: PkKill,
            SalvoSize: SalvoSize);

    public static ScenarioEngageDefaults MvpFallback { get; } = new(
        50_000,
        1_000,
        100_000,
        defaultMagazineRounds: 2,
        hasFireControlTrack: true);
}