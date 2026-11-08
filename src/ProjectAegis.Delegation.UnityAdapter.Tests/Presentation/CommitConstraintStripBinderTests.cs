using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>S124-01 W2-C2-01: pre-commit constraint + magazine cost strip.</summary>
[TestFixture]
public sealed class CommitConstraintStripBinderTests
{
    private static EngageContext Ctx(int rounds, int salvo = 2) =>
        ScenarioEngageDefaults.MvpFallback.ToEngageContext(roundsRemaining: rounds) with { SalvoSize = salvo };

    private static EngagePreview Preview(in EngageContext ctx) =>
        EngagePreviewProjection.Project(in ctx, ScenarioEngageDefaults.MvpFallback.DlzPersonality);

    [Test]
    public void Ready_salvo_commit_shows_no_constraints_and_salvo_magazine_cost()
    {
        var ctx = Ctx(rounds: 8, salvo: 2);

        var strip = CommitConstraintStripBinder.Bind("fire-salvo", in ctx, Preview(in ctx));

        Assert.That(strip.CanCommit, Is.True);
        Assert.That(strip.Constraints, Is.Empty);
        Assert.That(strip.MagazineCost, Is.EqualTo(2));
        Assert.That(strip.RoundsBefore, Is.EqualTo(8));
        Assert.That(strip.RoundsAfter, Is.EqualTo(6));
        Assert.That(strip.ConstraintLine, Is.EqualTo("COMMIT: READY"));
        Assert.That(strip.CostLine, Is.EqualTo("COST: 2 rd | MAG 8 -> 6"));
        Assert.That(strip.CueClass, Is.EqualTo(CommitConstraintStripCueClasses.Ready));
        Assert.That(strip.DeclutterToken, Is.EqualTo(CommitConstraintStripDeclutterTokens.Ready));
        Assert.That(strip.IsFireOrder, Is.False);
    }

    [Test]
    public void Single_round_commit_costs_one_round()
    {
        var ctx = Ctx(rounds: 3, salvo: 4);

        var strip = CommitConstraintStripBinder.Bind("fire-single", in ctx, Preview(in ctx));

        Assert.That(strip.MagazineCost, Is.EqualTo(1));
        Assert.That(strip.RoundsAfter, Is.EqualTo(2));
        Assert.That(strip.CostLine, Is.EqualTo("COST: 1 rd | MAG 3 -> 2"));
    }

    [Test]
    public void Hold_fire_commit_has_zero_cost_and_no_constraints_even_when_fire_is_blocked()
    {
        var ctx = Ctx(rounds: 0) with { MountOnline = false };

        var strip = CommitConstraintStripBinder.Bind("hold-fire", in ctx, Preview(in ctx));

        Assert.That(strip.CanCommit, Is.True);
        Assert.That(strip.Constraints, Is.Empty);
        Assert.That(strip.MagazineCost, Is.EqualTo(0));
        Assert.That(strip.CostLine, Is.EqualTo("COST: 0 rd | MAG 0 -> 0"));
    }

    [Test]
    public void Short_magazine_salvo_is_blocked_with_no_ammo_first_and_shortfall_in_cost()
    {
        var ctx = Ctx(rounds: 1, salvo: 2);

        var strip = CommitConstraintStripBinder.Bind("fire-salvo", in ctx, Preview(in ctx));

        Assert.That(strip.CanCommit, Is.False);
        Assert.That(strip.PrimaryReasonCode, Is.EqualTo("NO_AMMO"));
        Assert.That(strip.Constraints[0].Code, Is.EqualTo("NO_AMMO"));
        Assert.That(strip.Constraints[0].Source, Is.EqualTo(CommitConstraintSources.AttackOption));
        Assert.That(strip.Constraints[0].Rank, Is.EqualTo(1));
        Assert.That(strip.RoundsAfter, Is.EqualTo(0));
        Assert.That(strip.CostLine, Is.EqualTo("COST: 2 rd | MAG 1 -> 0 (short 1)"));
        Assert.That(strip.ConstraintLine, Does.StartWith("COMMIT: BLOCKED | NO_AMMO"));
        Assert.That(strip.CueClass, Is.EqualTo(CommitConstraintStripCueClasses.Blocked));
        Assert.That(strip.DeclutterToken, Is.EqualTo(CommitConstraintStripDeclutterTokens.Blocked));
    }

    [Test]
    public void Primary_reason_matches_command_facade_refusal_code()
    {
        var ctx = Ctx(rounds: 8) with { MountOnline = false };
        var preview = Preview(in ctx);

        var strip = CommitConstraintStripBinder.Bind("fire-single", in ctx, preview);
        var resolved = EngageAttackOrderResolver.TryResolve("fire-single", in ctx, preview, out _, out var failure);

        Assert.That(resolved, Is.False);
        Assert.That(strip.CanCommit, Is.False);
        Assert.That(strip.PrimaryReasonCode, Is.EqualTo(failure));
        Assert.That(strip.PrimaryReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.MOUNT_OFFLINE));
    }

    [Test]
    public void Constraints_merge_sources_in_fixed_order_dedupe_and_cap_at_top_n()
    {
        var ctx = Ctx(rounds: 8, salvo: 4) with { HasFireControlTrack = false };
        var preview = Preview(in ctx);
        var wra = WraSalvoRemainingBinder.Bind("u1", in ctx, new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2), preview);
        var tooltip = new WeaponAbortTooltipState(
            "WeaponsTight\nEmconOff",
            new[] { nameof(FireAbortReason.WeaponsTight), nameof(FireAbortReason.EmconOff) },
            true);

        var strip = CommitConstraintStripBinder.Bind("fire-salvo", in ctx, preview, wra: wra, abortTooltip: tooltip);

        Assert.That(CommitConstraintStripBinder.MaxConstraints, Is.EqualTo(3));
        Assert.That(strip.Constraints, Has.Count.EqualTo(3));
        Assert.That(strip.Constraints[0].Code, Is.EqualTo(AbortReasonCatalog.Engage.NO_FIRE_CONTROL_TRACK));
        Assert.That(strip.Constraints[0].Source, Is.EqualTo(CommitConstraintSources.AttackOption));
        Assert.That(strip.Constraints[1].Code, Is.EqualTo(AbortReasonCatalog.Doctrine.WRA_SALVO));
        Assert.That(strip.Constraints[1].Source, Is.EqualTo(CommitConstraintSources.WraSalvo));
        Assert.That(strip.Constraints[2].Code, Is.EqualTo(nameof(FireAbortReason.WeaponsTight)));
        Assert.That(strip.Constraints[2].Source, Is.EqualTo(CommitConstraintSources.AbortTooltip));
        Assert.That(strip.Constraints.Select(c => c.Rank), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(
            strip.ConstraintLine,
            Is.EqualTo("COMMIT: BLOCKED | NO_FIRE_CONTROL_TRACK | WRA_SALVO | WeaponsTight"));
    }

    [Test]
    public void Binding_is_deterministic_for_identical_inputs()
    {
        var ctx = Ctx(rounds: 1, salvo: 2) with { RadarEmconActive = false };
        var preview = Preview(in ctx);
        var wra = WraSalvoRemainingBinder.Bind("u1", in ctx, new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 1), preview);

        var a = CommitConstraintStripBinder.Bind("fire-salvo", in ctx, preview, wra: wra);
        var b = CommitConstraintStripBinder.Bind("fire-salvo", in ctx, preview, wra: wra);

        Assert.That(a.ConstraintLine, Is.EqualTo(b.ConstraintLine));
        Assert.That(a.CostLine, Is.EqualTo(b.CostLine));
        Assert.That(a.Constraints, Is.EqualTo(b.Constraints));
    }

    [Test]
    public void Enabled_commit_with_advisory_wra_constraint_stays_committable()
    {
        var ctx = Ctx(rounds: 8, salvo: 4);
        var preview = Preview(in ctx);
        var wra = WraSalvoRemainingBinder.Bind("u1", in ctx, new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2), preview);

        var strip = CommitConstraintStripBinder.Bind("fire-salvo", in ctx, preview, wra: wra);

        Assert.That(strip.CanCommit, Is.True);
        Assert.That(strip.PrimaryReasonCode, Is.Null);
        Assert.That(strip.Constraints.Single().Code, Is.EqualTo(AbortReasonCatalog.Doctrine.WRA_SALVO));
        Assert.That(strip.ConstraintLine, Is.EqualTo("COMMIT: READY | WRA_SALVO"));
        Assert.That(strip.CueClass, Is.EqualTo(CommitConstraintStripCueClasses.Ready));
    }

    [Test]
    public void Supplied_attack_menu_preview_is_bound_instead_of_rebuilding_options()
    {
        var ctx = Ctx(rounds: 8);
        var menu = AttackOptionsPreviewBinder.Bind(new[]
        {
            new EngageAttackOptions.AttackOption("fire-single", "Fire 1 round", false, "ROE_WEAPONS_TIGHT"),
        });

        var strip = CommitConstraintStripBinder.Bind("fire-single", in ctx, Preview(in ctx), attackMenu: menu);

        Assert.That(strip.CanCommit, Is.False);
        Assert.That(strip.PrimaryReasonCode, Is.EqualTo("ROE_WEAPONS_TIGHT"));
    }

    [Test]
    public void Unknown_option_is_blocked_with_unknown_option_code_and_zero_cost()
    {
        var ctx = Ctx(rounds: 8);

        var strip = CommitConstraintStripBinder.Bind("fire-everything", in ctx, Preview(in ctx));

        Assert.That(strip.CanCommit, Is.False);
        Assert.That(strip.PrimaryReasonCode, Is.EqualTo(CommitConstraintStripBinder.UnknownOptionCode));
        Assert.That(strip.MagazineCost, Is.EqualTo(0));
    }

    [Test]
    public void Bind_rows_maps_constraint_and_cost_lines_to_strip_elements()
    {
        var ctx = Ctx(rounds: 1, salvo: 2);
        var strip = CommitConstraintStripBinder.Bind("fire-salvo", in ctx, Preview(in ctx));

        var rows = CommitConstraintStripBinder.BindRows(strip);

        Assert.That(rows.Select(r => r.ElementName), Is.EqualTo(new[]
        {
            CommitConstraintStripBinder.ConstraintElementName,
            CommitConstraintStripBinder.CostElementName,
        }));
        Assert.That(rows[0].Text, Is.EqualTo(strip.ConstraintLine));
        Assert.That(rows[1].Text, Is.EqualTo(strip.CostLine));
        Assert.That(rows.All(r => r.CueClass == strip.CueClass), Is.True);
    }

    [Test]
    public void Empty_state_is_blocked_placeholder()
    {
        var empty = CommitConstraintStripState.Empty;

        Assert.That(empty.CanCommit, Is.False);
        Assert.That(empty.Constraints, Is.Empty);
        Assert.That(empty.CueClass, Is.EqualTo(CommitConstraintStripCueClasses.Empty));
        Assert.That(empty.IsFireOrder, Is.False);
    }
}
