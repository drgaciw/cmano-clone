namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Engage;

/// <summary>Headless attack-options menu entries (CMO §4.1.1 / req 14).</summary>
public static class EngageAttackOptions
{
    public sealed record AttackOption(string Id, string Label, bool Enabled, string? DisabledReason = null);

    public static IReadOnlyList<AttackOption> Build(in EngageContext ctx, EngagePreview preview)
    {
        var salvo = Math.Max(1, ctx.SalvoSize);
        var canFire = preview.CanFire;
        var abort = preview.AbortPreviewCode;

        return
        [
            new AttackOption(
                "fire-single",
                "Fire 1 round",
                canFire && ctx.RoundsRemaining > 0,
                canFire ? null : abort),
            new AttackOption(
                "fire-salvo",
                $"Fire salvo ({salvo})",
                canFire && ctx.RoundsRemaining >= salvo,
                canFire ? (ctx.RoundsRemaining < salvo ? "NO_AMMO" : null) : abort),
            new AttackOption(
                "hold-fire",
                "Hold fire",
                true,
                null),
        ];
    }
}