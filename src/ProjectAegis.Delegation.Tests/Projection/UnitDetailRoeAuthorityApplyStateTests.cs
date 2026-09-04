using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// S122-07 / DRG-182: selected-unit ROE + authority chrome on RightUnitDetail.
/// Headless format/apply contract — no hex color, no new drawer.
/// </summary>
public sealed class UnitDetailRoeAuthorityApplyStateTests
{
    [Test]
    public void FormatRoeLine_weapons_free_is_prefixed_contract_label()
    {
        Assert.That(
            UnitDetailApplyState.FormatRoeLine(RoeLevel.WeaponsFree),
            Is.EqualTo("ROE: WEAPONS_FREE"));
    }

    [Test]
    public void FormatRoeLine_hold_fire_is_prefixed_contract_label()
    {
        Assert.That(
            UnitDetailApplyState.FormatRoeLine(RoeLevel.HoldFire),
            Is.EqualTo("ROE: HOLD_FIRE"));
    }

    [Test]
    public void FormatRoeLine_weapons_tight_is_prefixed_contract_label()
    {
        Assert.That(
            UnitDetailApplyState.FormatRoeLine(RoeLevel.WeaponsTight),
            Is.EqualTo("ROE: WEAPONS_TIGHT"));
    }

    [Test]
    public void FormatRoeLine_null_is_empty_placeholder()
    {
        Assert.That(UnitDetailApplyState.FormatRoeLine((RoeLevel?)null), Is.EqualTo("ROE: —"));
    }

    [Test]
    public void FormatRoeLine_doctrine_label_parses_existing_unit_detail_field()
    {
        Assert.That(
            UnitDetailApplyState.FormatRoeLine("DOCTRINE: WeaponsFree"),
            Is.EqualTo("ROE: WEAPONS_FREE"));
        Assert.That(
            UnitDetailApplyState.FormatRoeLine("DOCTRINE: WeaponsTight (mission)"),
            Is.EqualTo("ROE: WEAPONS_TIGHT"));
        Assert.That(
            UnitDetailApplyState.FormatRoeLine("DOCTRINE: HoldFire"),
            Is.EqualTo("ROE: HOLD_FIRE"));
        Assert.That(
            UnitDetailApplyState.FormatRoeLine("DOCTRINE: —"),
            Is.EqualTo("ROE: —"));
        Assert.That(UnitDetailApplyState.FormatRoeLine((string?)null), Is.EqualTo("ROE: —"));
    }

    [Test]
    public void FormatAuthorityLine_organic_track_source()
    {
        Assert.That(
            UnitDetailApplyState.FormatAuthorityLine(TrackSource.Organic),
            Is.EqualTo("AUTH: ORGANIC"));
    }

    [Test]
    public void FormatAuthorityLine_projector_disposition_text()
    {
        Assert.That(
            UnitDetailApplyState.FormatAuthorityLine(C2AuthorityDisposition.Permitted),
            Is.EqualTo("AUTH: Permitted"));
        Assert.That(
            UnitDetailApplyState.FormatAuthorityLine(C2AuthorityDisposition.Withheld),
            Is.EqualTo("AUTH: Withheld"));
    }

    [Test]
    public void FormatAuthorityLine_empty_is_placeholder()
    {
        Assert.That(UnitDetailApplyState.FormatAuthorityLine((TrackSource?)null), Is.EqualTo("AUTH: —"));
        Assert.That(
            UnitDetailApplyState.FormatAuthorityLine((C2AuthorityDisposition?)null),
            Is.EqualTo("AUTH: —"));
        Assert.That(UnitDetailApplyState.FormatAuthorityLine(TrackSource.Unknown), Is.EqualTo("AUTH: —"));
    }

    [Test]
    public void ProjectSelected_weapons_free_authority_is_permitted()
    {
        var entry = ProjectSelectedWithRoe(RoeLevel.WeaponsFree);

        Assert.That(entry.AuthorityLabel, Is.EqualTo("AUTH: Permitted"));
        Assert.That(entry.AuthorityLabel, Does.Not.Contain("ORGANIC"));

        var applied = UnitDetailApplyState.BindAndApply(entry);
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: Permitted"));
    }

    [Test]
    public void ProjectSelected_hold_fire_authority_is_withheld()
    {
        var entry = ProjectSelectedWithRoe(RoeLevel.HoldFire);

        Assert.That(entry.AuthorityLabel, Is.EqualTo("AUTH: Withheld"));

        var applied = UnitDetailApplyState.BindAndApply(entry);
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: Withheld"));
    }

    [Test]
    public void ProjectSelected_weapons_tight_authority_is_withheld()
    {
        var entry = ProjectSelectedWithRoe(RoeLevel.WeaponsTight);

        Assert.That(entry.AuthorityLabel, Is.EqualTo("AUTH: Withheld"));

        var applied = UnitDetailApplyState.BindAndApply(entry);
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: Withheld"));
    }

    [Test]
    public void ProjectSelected_null_policy_authority_is_placeholder()
    {
        var entry = UnitDetailProjection.ProjectSelected(
            new TargetId("u1"),
            _ => true,
            new DecisionLog(),
            policy: null,
            simTimeSeconds: 0);

        Assert.That(entry!.AuthorityLabel, Is.EqualTo("AUTH: —"));

        var applied = UnitDetailApplyState.BindAndApply(entry);
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: —"));
    }

    [Test]
    public void Bind_formats_existing_authority_label_strings()
    {
        var organic = UnitDetailPanelBinder.Bind(
            SampleEntry(DoctrineLabel: "DOCTRINE: WeaponsFree", AuthorityLabel: "AUTH: ORGANIC"));
        Assert.That(organic.AuthorityLine, Is.EqualTo("AUTH: ORGANIC"));

        var permitted = UnitDetailPanelBinder.Bind(
            SampleEntry(DoctrineLabel: "DOCTRINE: WeaponsFree", AuthorityLabel: "AUTH: Permitted"));
        Assert.That(permitted.AuthorityLine, Is.EqualTo("AUTH: Permitted"));

        var withheld = UnitDetailPanelBinder.Bind(
            SampleEntry(DoctrineLabel: "DOCTRINE: HoldFire", AuthorityLabel: "AUTH: Withheld"));
        Assert.That(withheld.AuthorityLine, Is.EqualTo("AUTH: Withheld"));

        var placeholder = UnitDetailPanelBinder.Bind(
            SampleEntry(DoctrineLabel: "DOCTRINE: —", AuthorityLabel: "AUTH: —"));
        Assert.That(placeholder.AuthorityLine, Is.EqualTo("AUTH: —"));
    }

    [Test]
    public void BindAndApply_maps_doctrine_label_to_roe_line()
    {
        var entry = SampleEntry(DoctrineLabel: "DOCTRINE: WeaponsFree", AuthorityLabel: "AUTH: ORGANIC");

        var applied = UnitDetailApplyState.BindAndApply(entry);

        Assert.That(applied.DoctrineLine, Is.EqualTo("ROE: WEAPONS_FREE"));
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: ORGANIC"));
        Assert.That(applied.DoctrineLine, Does.Not.Match("#[0-9A-Fa-f]{3,8}"));
        Assert.That(applied.AuthorityLine, Does.Not.Match("#[0-9A-Fa-f]{3,8}"));
    }

    [Test]
    public void BindAndApply_hold_fire_and_withheld_disposition()
    {
        var entry = SampleEntry(
            DoctrineLabel: "DOCTRINE: HoldFire",
            AuthorityLabel: UnitDetailApplyState.FormatAuthorityLine(C2AuthorityDisposition.Withheld));

        var applied = UnitDetailApplyState.BindAndApply(entry);

        Assert.That(applied.DoctrineLine, Is.EqualTo("ROE: HOLD_FIRE"));
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: Withheld"));
    }

    [Test]
    public void BindAndApply_weapons_tight_and_permitted_disposition()
    {
        var entry = SampleEntry(
            DoctrineLabel: "DOCTRINE: WeaponsTight",
            AuthorityLabel: UnitDetailApplyState.FormatAuthorityLine(C2AuthorityDisposition.Permitted));

        var applied = UnitDetailApplyState.BindAndApply(entry);

        Assert.That(applied.DoctrineLine, Is.EqualTo("ROE: WEAPONS_TIGHT"));
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: Permitted"));
    }

    [Test]
    public void BindAndApply_empty_entry_uses_placeholders()
    {
        var applied = UnitDetailApplyState.BindAndApply(null);

        Assert.That(applied.DoctrineLine, Is.EqualTo("ROE: —"));
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: —"));
    }

    [Test]
    public void Apply_null_empty_presentation_uses_placeholders()
    {
        var applied = UnitDetailApplyState.Apply(null);

        Assert.That(applied.DoctrineLine, Is.EqualTo("ROE: —"));
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: —"));
    }

    [Test]
    public void Apply_copies_authority_line_from_panel_state()
    {
        var bound = UnitDetailPanelBinder.Bind(SampleEntry(
            DoctrineLabel: "DOCTRINE: WeaponsFree",
            AuthorityLabel: "AUTH: ORGANIC"));
        var applied = UnitDetailApplyState.Apply(bound);

        Assert.That(applied.DoctrineLine, Is.EqualTo(bound.DoctrineLine));
        Assert.That(applied.AuthorityLine, Is.EqualTo(bound.AuthorityLine));
        Assert.That(applied.DoctrineLine, Is.EqualTo("ROE: WEAPONS_FREE"));
        Assert.That(applied.AuthorityLine, Is.EqualTo("AUTH: ORGANIC"));
    }

    [Test]
    public void UnitDetailPanel_uxml_has_roe_and_authority_lines_on_existing_root()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);
        var uxml = Path.Combine(root!, "unity", "ProjectAegis", "Assets", "UI", "UnitDetail", "UnitDetailPanel.uxml");
        Assert.That(File.Exists(uxml), Is.True, uxml);

        var text = File.ReadAllText(uxml);
        Assert.That(text, Does.Contain("name=\"unit-detail-root\""));
        Assert.That(text, Does.Contain("name=\"doctrine-line\""));
        Assert.That(text, Does.Contain("text=\"ROE: —\""));
        Assert.That(text, Does.Contain("name=\"authority-line\""));
        Assert.That(text, Does.Contain("text=\"AUTH: —\""));
        Assert.That(text, Does.Not.Contain("text=\"DOCTRINE: —\""));
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var marker = Path.Combine(dir.FullName, "unity", "ProjectAegis", "Assets");
            if (Directory.Exists(marker))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static UnitDetailEntry ProjectSelectedWithRoe(RoeLevel roe)
    {
        var policy = new ScenarioPolicyProfile(new EffectivePolicy(roe));
        return UnitDetailProjection.ProjectSelected(
            new TargetId("u1"),
            _ => true,
            new DecisionLog(),
            policy,
            simTimeSeconds: 0)!;
    }

    private static UnitDetailEntry SampleEntry(string DoctrineLabel, string AuthorityLabel) =>
        new(
            "blue-1",
            IsAlive: true,
            StatusLabel: "OPERATIONAL",
            MagazineLabel: "MAGAZINE: —",
            EmconLabel: "EMCON: —",
            DoctrineLabel: DoctrineLabel,
            FuelLabel: "FUEL: —",
            EngagePreviewLabel: "ENGAGE: —",
            AttackOptionsLabel: "ATTACK: —",
            AttackMenu: Array.Empty<EngageAttackOptions.AttackOption>(),
            CommsLabel: "COMMS: —",
            AuthorityLabel: AuthorityLabel);
}
