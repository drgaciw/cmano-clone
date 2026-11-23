using ProjectAegis.Delegation.CombatEvents;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Provenance of combat-picture timing capture (DRG-176 honest boundary: headless bind vs Unity frame).</summary>
public enum CombatFrameCaptureSource
{
    HeadlessBindMeasurement = 1,
    UnityFrameCapture = 2,
}

/// <summary>Declutter and bind-performance budget for one zoom band (DRG-176).</summary>
public sealed record CombatPictureZoomBandBudget(
    CombatZoomBand ZoomBand,
    int MaxVisibleLabels,
    int MaxVisibleBattleLegs,
    int MaxVisibleOverlays,
    double MaxMeanBindMilliseconds,
    double MaxP95BindMilliseconds,
    long MaxGcAllocBytesPerBind);

/// <summary>Measured combat-picture telemetry sample evaluated against <see cref="CombatPictureZoomBandBudget"/>.</summary>
public sealed record CombatPictureBudgetSample(
    CombatZoomBand ZoomBand,
    CombatFrameCaptureSource CaptureSource,
    string Resolution,
    int EntityCount,
    int VisibleLabelCount,
    int VisibleBattleLegCount,
    int VisibleOverlayCount,
    double MeanBindMilliseconds,
    double P95BindMilliseconds,
    long GcAllocBytesPerBind);

/// <summary>Deterministic budget evaluation result for DRG-176.</summary>
public sealed record CombatPictureBudgetEvaluation(
    bool Passed,
    bool ProvesUnityFrameTiming,
    CombatPictureZoomBandBudget BudgetApplied,
    IReadOnlyList<string> Violations);

/// <summary>Canonical zoom-band declutter and performance budgets (DRG-176).</summary>
public static class CombatPictureDeclutterBudgets
{
    public static CombatPictureZoomBandBudget Theater { get; } = new(
        CombatZoomBand.Theater,
        MaxVisibleLabels: 12,
        MaxVisibleBattleLegs: 4,
        MaxVisibleOverlays: 4,
        MaxMeanBindMilliseconds: 2.0,
        MaxP95BindMilliseconds: 4.0,
        MaxGcAllocBytesPerBind: 32_768);

    public static CombatPictureZoomBandBudget Operational { get; } = new(
        CombatZoomBand.Operational,
        MaxVisibleLabels: 24,
        MaxVisibleBattleLegs: 12,
        MaxVisibleOverlays: 10,
        MaxMeanBindMilliseconds: 3.0,
        MaxP95BindMilliseconds: 6.0,
        MaxGcAllocBytesPerBind: 65_536);

    public static CombatPictureZoomBandBudget Tactical { get; } = new(
        CombatZoomBand.Tactical,
        MaxVisibleLabels: 48,
        MaxVisibleBattleLegs: 24,
        MaxVisibleOverlays: 16,
        MaxMeanBindMilliseconds: 4.0,
        MaxP95BindMilliseconds: 8.0,
        MaxGcAllocBytesPerBind: 131_072);

    /// <summary>Returns the canonical declutter and performance budget for a zoom band.</summary>
    public static CombatPictureZoomBandBudget ForZoomBand(CombatZoomBand zoomBand) => zoomBand switch
    {
        CombatZoomBand.Theater => Theater,
        CombatZoomBand.Operational => Operational,
        _ => Tactical,
    };
}

/// <summary>Evaluates combat-picture density and bind timing against DRG-176 budgets.</summary>
public static class CombatPictureDeclutterEvaluator
{
    /// <summary>Evaluates a measured sample against its zoom-band declutter and timing budget.</summary>
    public static CombatPictureBudgetEvaluation Evaluate(CombatPictureBudgetSample sample)
    {
        if (sample is null)
        {
            throw new ArgumentNullException(nameof(sample));
        }

        var budget = CombatPictureDeclutterBudgets.ForZoomBand(sample.ZoomBand);
        var violations = new List<string>();

        if (sample.VisibleLabelCount > budget.MaxVisibleLabels)
        {
            violations.Add($"Visible labels {sample.VisibleLabelCount} > {budget.MaxVisibleLabels} ({sample.ZoomBand}).");
        }

        if (sample.VisibleBattleLegCount > budget.MaxVisibleBattleLegs)
        {
            violations.Add($"Visible battle legs {sample.VisibleBattleLegCount} > {budget.MaxVisibleBattleLegs} ({sample.ZoomBand}).");
        }

        if (sample.VisibleOverlayCount > budget.MaxVisibleOverlays)
        {
            violations.Add($"Visible overlays {sample.VisibleOverlayCount} > {budget.MaxVisibleOverlays} ({sample.ZoomBand}).");
        }

        if (sample.MeanBindMilliseconds > budget.MaxMeanBindMilliseconds)
        {
            violations.Add($"Mean bind {sample.MeanBindMilliseconds:F2}ms > {budget.MaxMeanBindMilliseconds:F2}ms.");
        }

        if (sample.P95BindMilliseconds > budget.MaxP95BindMilliseconds)
        {
            violations.Add($"p95 bind {sample.P95BindMilliseconds:F2}ms > {budget.MaxP95BindMilliseconds:F2}ms.");
        }

        if (sample.GcAllocBytesPerBind > budget.MaxGcAllocBytesPerBind)
        {
            violations.Add($"GC allocation {sample.GcAllocBytesPerBind}B > {budget.MaxGcAllocBytesPerBind}B.");
        }

        return new CombatPictureBudgetEvaluation(
            Passed: violations.Count == 0,
            ProvesUnityFrameTiming: sample.CaptureSource == CombatFrameCaptureSource.UnityFrameCapture,
            BudgetApplied: budget,
            Violations: violations);
    }

    /// <summary>Evaluates a projected <see cref="BattleGraphicState"/> against its zoom-band battle-leg budget.</summary>
    public static CombatPictureBudgetEvaluation EvaluateBattleGraphic(
        BattleGraphicState state,
        CombatFrameCaptureSource captureSource = CombatFrameCaptureSource.HeadlessBindMeasurement,
        string resolution = "1600x979")
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var legCount = Math.Max(state.VisibleCount, state.Legs?.Count ?? 0);
        return Evaluate(new CombatPictureBudgetSample(
            ZoomBand: state.Zoom,
            CaptureSource: captureSource,
            Resolution: resolution,
            EntityCount: legCount,
            VisibleLabelCount: legCount,
            VisibleBattleLegCount: legCount,
            VisibleOverlayCount: legCount > 0 ? 1 : 0,
            MeanBindMilliseconds: 0.0,
            P95BindMilliseconds: 0.0,
            GcAllocBytesPerBind: 0));
    }
}
