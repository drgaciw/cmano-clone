using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class ControllerBadgeBinderTests
{
    [Test]
    public void BindRoster_maps_human_agent_suspended_and_hides_none()
    {
        var bindings = ControllerBadgeBinder.BindRoster(new[]
        {
            Entry("u-human", "Human", null),
            Entry("u-agent", "Agent", "a1"),
            Entry("u-susp", "AgentSuspended", "a2"),
            Entry("u-none", "None", null),
        });

        Assert.That(ControllerBadgeBinder.TryGet(bindings, "u-human", out var human), Is.True);
        Assert.That(human!.Visible, Is.True);
        Assert.That(human.ShortText, Is.EqualTo("HUM"));
        Assert.That(human.ColorClass, Is.EqualTo("badge-human"));
        Assert.That(human.ShapeClass, Is.EqualTo("badge-shape-square"));
        Assert.That(human.Tooltip, Is.EqualTo("Human control"));
        Assert.That(human.OobSuffix, Is.EqualTo(" [HUM]"));

        Assert.That(ControllerBadgeBinder.TryGet(bindings, "u-agent", out var agent), Is.True);
        Assert.That(agent!.ShortText, Is.EqualTo("AGT"));
        Assert.That(agent.ColorClass, Is.EqualTo("badge-agent"));
        Assert.That(agent.ShapeClass, Is.EqualTo("badge-shape-diamond"));
        Assert.That(agent.Tooltip, Does.Contain("a1"));

        Assert.That(ControllerBadgeBinder.TryGet(bindings, "u-susp", out var suspended), Is.True);
        Assert.That(suspended!.ShortText, Is.EqualTo("AGT-SUSP"));
        Assert.That(suspended.ColorClass, Is.EqualTo("badge-agent-suspended"));
        Assert.That(suspended.ShapeClass, Is.EqualTo("badge-shape-diamond-dashed"));
        Assert.That(suspended.Tooltip, Does.Contain("a2"));

        Assert.That(ControllerBadgeBinder.TryGet(bindings, "u-none", out var none), Is.True);
        Assert.That(none!.Visible, Is.False);
        Assert.That(none.ShortText, Is.EqualTo(AgentRosterProjection.MissingAgentId));
    }

    [Test]
    public void Human_with_held_agent_names_the_suspended_agent_in_the_tooltip()
    {
        var bindings = ControllerBadgeBinder.BindRoster(new[] { Entry("u1", "Human", "a1") });

        Assert.That(ControllerBadgeBinder.TryGet(bindings, "u1", out var binding), Is.True);
        Assert.That(binding!.Tooltip, Is.EqualTo("Human control (agent a1 suspended)"));
        Assert.That(binding.Visible, Is.True);
    }

    [Test]
    public void DecorateOobLine_appends_visible_short_text_and_leaves_hidden_rows_unchanged()
    {
        var bindings = ControllerBadgeBinder.BindRoster(new[]
        {
            Entry("u1", "Human", null),
            Entry("u2", "None", null),
        });
        ControllerBadgeBinder.TryGet(bindings, "u1", out var human);
        ControllerBadgeBinder.TryGet(bindings, "u2", out var none);

        Assert.That(ControllerBadgeBinder.DecorateOobLine("u1 READY", human), Is.EqualTo("u1 READY [HUM]"));
        Assert.That(ControllerBadgeBinder.DecorateOobLine("u2 READY", none), Is.EqualTo("u2 READY"));
        Assert.That(ControllerBadgeBinder.DecorateOobLine("u3 READY", null), Is.EqualTo("u3 READY"));
        Assert.That(ControllerBadgeBinder.DecorateOobLine(null, human), Is.EqualTo(" [HUM]"));
    }

    [Test]
    public void Map_symbol_bind_accepts_friendly_and_friendly_dead_only()
    {
        var bindings = ControllerBadgeBinder.BindRoster(new[]
        {
            Entry("u1", "Agent", "a1"),
            Entry("hostile-1", "Agent", "a9"),
        });

        Assert.That(
            ControllerBadgeBinder.TryBindMapSymbol("u1", "map-symbol--friendly", bindings, out var friendly),
            Is.True);
        Assert.That(friendly!.ShortText, Is.EqualTo("AGT"));

        Assert.That(
            ControllerBadgeBinder.TryBindMapSymbol(
                "u1",
                "map-symbol--friendly-dead map-symbol--selected",
                bindings,
                out var dead),
            Is.True);
        Assert.That(dead!.UnitId, Is.EqualTo("u1"));

        Assert.That(
            ControllerBadgeBinder.TryBindMapSymbol("hostile-1", "map-symbol--hostile", bindings, out _),
            Is.False);
        Assert.That(
            ControllerBadgeBinder.TryBindMapSymbol("missing", "map-symbol--friendly", bindings, out _),
            Is.False);
        Assert.That(
            ControllerBadgeBinder.TryBindMapSymbol("u1", "map-symbol--neutral", bindings, out _),
            Is.False);
        Assert.That(ControllerBadgeBinder.IsFriendlyMapSymbol("map-symbol--friendlyextra"), Is.False);
    }

    [Test]
    public void None_badge_does_not_bind_a_friendly_symbol()
    {
        var bindings = ControllerBadgeBinder.BindRoster(new[] { Entry("u1", "None", null) });

        Assert.That(
            ControllerBadgeBinder.TryBindMapSymbol("u1", "map-symbol--friendly", bindings, out _),
            Is.False);
    }

    [Test]
    public void Null_or_empty_roster_returns_an_empty_lookup()
    {
        Assert.That(ControllerBadgeBinder.BindRoster(null), Is.Empty);
        Assert.That(ControllerBadgeBinder.BindRoster(Array.Empty<AgentRosterEntry>()), Is.Empty);
        Assert.That(ControllerBadgeBinder.TryGet(null, "u1", out _), Is.False);
        Assert.That(ControllerBadgeBinder.RosterStamp(null), Is.EqualTo(0));
        Assert.That(ControllerBadgeBinder.RosterStamp(Array.Empty<AgentRosterEntry>()), Is.EqualTo(0));
    }

    [Test]
    public void RosterStamp_tracks_content_not_list_identity()
    {
        var left = new[] { Entry("u1", "Agent", "a1") };
        var right = new[] { Entry("u1", "Agent", "a1") };
        var changed = new[] { Entry("u1", "Human", "a1") };

        Assert.That(ControllerBadgeBinder.RosterStamp(left), Is.EqualTo(ControllerBadgeBinder.RosterStamp(right)));
        Assert.That(ControllerBadgeBinder.RosterStamp(left), Is.Not.EqualTo(ControllerBadgeBinder.RosterStamp(changed)));
        Assert.That(ControllerBadgeBinder.NeedsRebind(true, ControllerBadgeBinder.RosterStamp(left), left, right), Is.False);
        Assert.That(ControllerBadgeBinder.NeedsRebind(true, ControllerBadgeBinder.RosterStamp(left), left, changed), Is.True);
    }

    [Test]
    public void Colliding_32_bit_mix_still_rebinds_when_agent_ids_differ()
    {
        var left = new[] { Entry("u1", "Agent", "friendly-Aa") };
        var right = new[] { Entry("u1", "Agent", "friendly-BB") };

        Assert.That(ControllerBadgeBinder.RosterStamp(left), Is.EqualTo(ControllerBadgeBinder.RosterStamp(right)));
        Assert.That(ControllerBadgeBinder.SameRoster(left, right), Is.False);
        Assert.That(
            ControllerBadgeBinder.NeedsRebind(true, ControllerBadgeBinder.RosterStamp(left), left, right),
            Is.True);

        var rebound = ControllerBadgeBinder.BindRoster(right);
        Assert.That(ControllerBadgeBinder.TryGet(rebound, "u1", out var badge), Is.True);
        Assert.That(badge!.Tooltip, Does.Contain("friendly-BB"));
        Assert.That(badge.Tooltip, Does.Not.Contain("friendly-Aa"));
    }

    [Test]
    public void Historical_review_uses_as_of_roster_or_hides_when_unknown()
    {
        var live = new[] { Entry("u1", "Human", null) };
        var asOf = new[] { Entry("u1", "Agent", "a-then") };

        var unknown = ControllerBadgeBinder.RosterForBadges(inspecting: true, live, asOfRoster: null);
        Assert.That(ControllerBadgeBinder.BindRoster(unknown), Is.Empty);
        Assert.That(
            ControllerBadgeBinder.TryBindMapSymbol("u1", "map-symbol--friendly", ControllerBadgeBinder.BindRoster(unknown), out _),
            Is.False);

        var historical = ControllerBadgeBinder.RosterForBadges(inspecting: true, live, asOf);
        Assert.That(ControllerBadgeBinder.TryGet(ControllerBadgeBinder.BindRoster(historical), "u1", out var past), Is.True);
        Assert.That(past!.ShortText, Is.EqualTo("AGT"));
        Assert.That(past.Tooltip, Does.Contain("a-then"));

        var commanding = ControllerBadgeBinder.RosterForBadges(inspecting: false, live, asOf);
        Assert.That(ControllerBadgeBinder.TryGet(ControllerBadgeBinder.BindRoster(commanding), "u1", out var now), Is.True);
        Assert.That(now!.ShortText, Is.EqualTo("HUM"));
    }

    [Test]
    public void ElementName_prefixes_the_unit_id()
    {
        Assert.That(ControllerBadgeBinder.ElementName("u1"), Is.EqualTo("controller-badge-u1"));
    }

    [Test]
    public void Hosts_bind_badges_without_opening_sim_authority()
    {
        var map = UiIaSourceReader.ReadRuntime("MapPlaceholderPanelHost.cs");
        var oob = UiIaSourceReader.ReadRuntime("OobTreePanelHost.cs");

        Assert.That(map, Does.Contain("ControllerBadgeBinder"));
        Assert.That(map, Does.Contain("ApplyControllerBadges"));
        Assert.That(map, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(map, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(map, Does.Not.Contain("SimulationSession"));
        Assert.That(Slice(map, "private bool IsDirty()", "private void CaptureDirtyState"), Does.Not.Contain("LastAgentRoster"));
        var selector = Slice(map, "BadgeRoster()", "private void ApplyControllerBadges()");
        Assert.That(selector, Does.Contain("RosterForBadges"));
        Assert.That(selector, Does.Contain("CommandTimeline.IsInspecting"));
        Assert.That(selector, Does.Contain("asOfRoster: null"));

        var mapUss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "MapPlaceholder", "MapPlaceholderPanel.uss");
        var oobUss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "OobTree", "OobTreePanel.uss");
        Assert.That(mapUss, Does.Contain(".map-controller-badge"));
        Assert.That(mapUss, Does.Contain(".badge-human"));
        Assert.That(mapUss, Does.Contain(".badge-agent"));
        Assert.That(mapUss, Does.Contain(".badge-agent-suspended"));
        Assert.That(mapUss, Does.Contain(".badge-shape-square"));
        Assert.That(mapUss, Does.Contain(".badge-shape-diamond"));
        Assert.That(mapUss, Does.Contain(".badge-shape-diamond-dashed"));
        Assert.That(oobUss, Does.Contain(".oob-controller-badge"));
        Assert.That(oobUss, Does.Contain(".badge-human"));
        Assert.That(oobUss, Does.Contain(".badge-shape-diamond"));
        Assert.That(oobUss, Does.Contain(".badge-shape-diamond-dashed"));

        Assert.That(oob, Does.Contain("ControllerBadgeBinder"));
        Assert.That(oob, Does.Contain("label.focusable = true"));
        Assert.That(oob, Does.Contain("KeyboardDiscoverySurfaces.OobListElementName"));
        Assert.That(oob, Does.Contain("bridgeHost.SelectUnit"));
        Assert.That(oob, Does.Contain("OnOobRowKeyDown"));
    }

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = source.IndexOf(endMarker, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), startMarker);
        Assert.That(end, Is.GreaterThan(start), endMarker);
        return source.Substring(start, end - start);
    }

    private static AgentRosterEntry Entry(string unitId, string mode, string? agentId) =>
        new(
            agentId ?? AgentRosterProjection.MissingAgentId,
            unitId,
            "—",
            "—",
            AgentRosterProjection.DefaultAttentionLabel,
            mode);
}
