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
    public void Unchanged_menu_contents_return_the_cached_preview_instance()
    {
        var first = AttackOptionsPreviewBinder.Bind(CacheMenu());
        var second = AttackOptionsPreviewBinder.Bind(CacheMenu());
        var asList = AttackOptionsPreviewBinder.Bind(new List<EngageAttackOptions.AttackOption>(CacheMenu()));

        Assert.That(second, Is.SameAs(first));
        Assert.That(asList, Is.SameAs(first));
    }

    [Test]
    public void Unchanged_menu_rebind_allocates_nothing()
    {
        var menu = CacheMenu();
        var sameContents = CacheMenu();
        AttackOptionsPreviewBinder.Bind(menu);
        AttackOptionsPreviewBinder.Bind(sameContents);

        var before = GC.GetAllocatedBytesForCurrentThread();
        AttackOptionsPreviewState? last = null;
        for (var frame = 0; frame < 100; frame++)
        {
            last = AttackOptionsPreviewBinder.Bind(frame % 2 == 0 ? menu : sameContents);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.EqualTo(0));
        Assert.That(last!.Fingerprint, Is.EqualTo("atk:fire-single:0:DLZ_OUT;hold-fire:1:-"));
    }

    [Test]
    public void Changed_menu_contents_rebuild_the_preview()
    {
        var first = AttackOptionsPreviewBinder.Bind(CacheMenu());
        var changed = AttackOptionsPreviewBinder.Bind(new[]
        {
            new EngageAttackOptions.AttackOption("fire-single", "Fire 1 round", true),
            new EngageAttackOptions.AttackOption("hold-fire", "Hold fire", true),
        });
        var restored = AttackOptionsPreviewBinder.Bind(CacheMenu());

        Assert.That(changed, Is.Not.SameAs(first));
        Assert.That(changed.Fingerprint, Is.EqualTo("atk:fire-single:1:-;hold-fire:1:-"));
        Assert.That(changed.CueClass, Is.EqualTo(AttackOptionsCueClasses.Ready));
        Assert.That(restored.Fingerprint, Is.EqualTo(first.Fingerprint));
        Assert.That(restored.PreviewLine, Is.EqualTo(first.PreviewLine));
    }

    [Test]
    public void Mutating_the_caller_menu_after_bind_does_not_return_a_stale_preview()
    {
        var menu = CacheMenu();
        var first = AttackOptionsPreviewBinder.Bind(menu);

        menu[0] = new EngageAttackOptions.AttackOption("fire-single", "Fire 1 round", false, "NO_AMMO");
        var second = AttackOptionsPreviewBinder.Bind(menu);

        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.Rows[0].AbortReason, Is.EqualTo("NO_AMMO"));
        Assert.That(first.Rows[0].AbortReason, Is.EqualTo("DLZ_OUT"));
    }

    [Test]
    public void Cached_preview_rows_cannot_be_mutated_through_a_cast()
    {
        var bound = AttackOptionsPreviewBinder.Bind(CacheMenu());

        Assert.That(bound.Rows, Is.Not.InstanceOf<AttackOptionMenuRow[]>());
        var asList = (IList<AttackOptionMenuRow>)bound.Rows;
        Assert.Throws<NotSupportedException>(() => asList[0] = asList[1]);
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

    private static EngageAttackOptions.AttackOption[] CacheMenu() =>
        new[]
        {
            new EngageAttackOptions.AttackOption("fire-single", "Fire 1 round", false, "DLZ_OUT"),
            new EngageAttackOptions.AttackOption("hold-fire", "Hold fire", true),
        };
}
