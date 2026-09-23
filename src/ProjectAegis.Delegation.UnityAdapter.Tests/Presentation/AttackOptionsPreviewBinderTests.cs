using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class AttackOptionsPreviewBinderTests
{
    [Test]
    public void Bind_lists_the_same_options_the_engage_projection_builds()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(1);
        var preview = EngagePreviewProjection.Project(in ctx, defaults.DlzPersonality);
        var menu = EngageAttackOptions.Build(in ctx, preview);

        var bound = AttackOptionsPreviewBinder.Bind(menu);

        Assert.That(bound.Rows, Has.Count.EqualTo(menu.Count));
        for (var i = 0; i < menu.Count; i++)
        {
            Assert.That(bound.Rows[i].OptionId, Is.EqualTo(menu[i].Id));
            Assert.That(bound.Rows[i].Label, Is.EqualTo(menu[i].Label));
            Assert.That(bound.Rows[i].Enabled, Is.EqualTo(menu[i].Enabled));
            Assert.That(
                bound.Rows[i].ButtonName,
                Is.EqualTo(AttackMenuPanelBinder.ResolveButtonName(menu[i].Id)));
            if (!menu[i].Enabled)
            {
                var reason = menu[i].DisabledReason ?? AttackOptionsPreviewBinder.MissingAbortToken;
                Assert.That(bound.Rows[i].AbortReason, Is.EqualTo(reason));
                Assert.That(bound.PreviewLine, Does.Contain(reason));
                Assert.That(bound.Rows[i].ButtonText, Does.Contain(reason));
            }
        }
    }

    [Test]
    public void Disabled_option_shows_projection_abort_reason_on_preview_and_button()
    {
        var menu = new[]
        {
            new EngageAttackOptions.AttackOption("fire-single", "Fire 1 round", true),
            new EngageAttackOptions.AttackOption("fire-salvo", "Fire salvo (2)", false, "NO_AMMO"),
            new EngageAttackOptions.AttackOption("hold-fire", "Hold fire", true),
        };

        var bound = AttackOptionsPreviewBinder.Bind(menu);

        Assert.That(
            bound.PreviewLine,
            Is.EqualTo("ATTACK: Fire 1 round | Fire salvo (2) (NO_AMMO) | Hold fire"));
        Assert.That(bound.Rows[1].ButtonText, Is.EqualTo("Fire salvo (2) (NO_AMMO)"));
        Assert.That(bound.Rows[1].AbortReason, Is.EqualTo("NO_AMMO"));
        Assert.That(bound.Rows[1].ButtonName, Is.EqualTo("attack-fire-salvo"));
        Assert.That(bound.Rows[1].CueClass, Is.EqualTo(AttackOptionsCueClasses.Blocked));
        Assert.That(bound.Rows[0].CueClass, Is.EqualTo(AttackOptionsCueClasses.Ready));
        Assert.That(bound.CueClass, Is.EqualTo(AttackOptionsCueClasses.Blocked));
    }

    [Test]
    public void Disabled_option_without_reason_shows_blocked_token()
    {
        var menu = new[]
        {
            new EngageAttackOptions.AttackOption("fire-single", "Fire 1 round", false),
        };

        var bound = AttackOptionsPreviewBinder.Bind(menu);

        Assert.That(bound.Rows[0].AbortReason, Is.EqualTo(AttackOptionsPreviewBinder.MissingAbortToken));
        Assert.That(bound.Rows[0].ButtonText, Is.EqualTo("Fire 1 round (BLOCKED)"));
        Assert.That(bound.PreviewLine, Is.EqualTo("ATTACK: Fire 1 round (BLOCKED)"));
    }

    [Test]
    public void Bind_is_replay_stable_for_the_same_menu()
    {
        var menu = new[]
        {
            new EngageAttackOptions.AttackOption("fire-single", "Fire 1 round", false, "DLZ_OUT"),
            new EngageAttackOptions.AttackOption("hold-fire", "Hold fire", true),
        };

        var first = AttackOptionsPreviewBinder.Bind(menu);
        var second = AttackOptionsPreviewBinder.Bind(menu);

        Assert.That(second.PreviewLine, Is.EqualTo(first.PreviewLine));
        Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
        Assert.That(second.CueClass, Is.EqualTo(first.CueClass));
        Assert.That(first.Fingerprint, Is.EqualTo("atk:fire-single:0:DLZ_OUT;hold-fire:1:-"));
    }

    [Test]
    public void Empty_and_null_menus_fail_closed()
    {
        Assert.That(AttackOptionsPreviewBinder.Bind(null), Is.SameAs(AttackOptionsPreviewState.Empty));
        Assert.That(
            AttackOptionsPreviewBinder.Bind(Array.Empty<EngageAttackOptions.AttackOption>()),
            Is.SameAs(AttackOptionsPreviewState.Empty));
        Assert.That(AttackOptionsPreviewState.Empty.PreviewLine, Is.EqualTo("ATTACK: —"));
        Assert.That(AttackOptionsPreviewState.Empty.CueClass, Is.EqualTo(AttackOptionsCueClasses.Empty));
    }

    [Test]
    public void Unknown_option_stays_on_the_preview_without_a_button_name()
    {
        var menu = new[]
        {
            new EngageAttackOptions.AttackOption("custom", "Custom", false, "ROE"),
        };

        var bound = AttackOptionsPreviewBinder.Bind(menu);

        Assert.That(bound.Rows, Has.Count.EqualTo(1));
        Assert.That(bound.Rows[0].ButtonName, Is.Null);
        Assert.That(bound.PreviewLine, Is.EqualTo("ATTACK: Custom (ROE)"));
        Assert.That(AttackOptionsPreviewBinder.FindRow(bound, "custom")!.AbortReason, Is.EqualTo("ROE"));
        Assert.That(AttackOptionsPreviewBinder.FindRow(bound, "fire-single"), Is.Null);
    }

    [Test]
    public void All_enabled_menu_uses_ready_cue()
    {
        var menu = new[]
        {
            new EngageAttackOptions.AttackOption("hold-fire", "Hold fire", true),
        };

        var bound = AttackOptionsPreviewBinder.Bind(menu);

        Assert.That(bound.CueClass, Is.EqualTo(AttackOptionsCueClasses.Ready));
        Assert.That(bound.Rows[0].AbortReason, Is.Null);
        Assert.That(bound.PreviewLine, Is.EqualTo("ATTACK: Hold fire"));
    }
}
