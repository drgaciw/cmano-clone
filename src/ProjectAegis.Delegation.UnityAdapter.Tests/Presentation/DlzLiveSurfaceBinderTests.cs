using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Engage;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class DlzLiveSurfaceBinderTests
{
    [Test]
    public void Threat_range_assessment_binds_in_zone_with_range_and_cue()
    {
        var range = new ThreatRangeAssessment(
            50_000,
            1_000,
            100_000,
            DlzState.InZone,
            "DLZ: InZone (Normal)",
            true);

        var result = DlzLiveSurfaceBinder.Bind(range);

        Assert.That(result.StateLine, Does.Contain("DLZ: InZone (Normal)"));
        Assert.That(result.StateLine, Does.Contain("50000 m"));
        Assert.That(result.ShortLabel, Is.EqualTo("IN"));
        Assert.That(result.DeclutterToken, Is.EqualTo(DlzDeclutterTokens.In));
        Assert.That(result.CueClass, Is.EqualTo(DlzCueClasses.InZone));
        Assert.That(result.DlzState, Is.EqualTo(DlzState.InZone));
    }

    [Test]
    public void Engage_preview_binds_approaching_as_marginal_without_recomputing_sim()
    {
        var preview = new EngagePreview("DLZ: Approaching (Early)", CanFire: true, AbortPreviewCode: null);

        var result = DlzLiveSurfaceBinder.BindFromEngagePreview(preview);

        Assert.That(result.ShortLabel, Is.EqualTo("MARGINAL"));
        Assert.That(result.DeclutterToken, Is.EqualTo(DlzDeclutterTokens.Marginal));
        Assert.That(result.CueClass, Is.EqualTo(DlzCueClasses.Approaching));
        Assert.That(result.DlzState, Is.EqualTo(DlzState.Approaching));
    }

    [Test]
    public void Engage_preview_binds_out_of_zone_with_out_cue()
    {
        var preview = new EngagePreview("DLZ: OutOfZone (Normal)", false, "DLZ_OUT");

        var result = DlzLiveSurfaceBinder.BindFromEngagePreview(preview);

        Assert.That(result.ShortLabel, Is.EqualTo("OUT"));
        Assert.That(result.DeclutterToken, Is.EqualTo(DlzDeclutterTokens.Out));
        Assert.That(result.CueClass, Is.EqualTo(DlzCueClasses.OutOfZone));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Missing_contact_selection_fails_closed(string? contactId)
    {
        var range = new ThreatRangeAssessment(1, 1, 2, DlzState.InZone, "DLZ: InZone (Normal)", true);
        Assert.That(
            DlzLiveSurfaceBinder.BindContact(contactId, range, null),
            Is.EqualTo(DlzLiveSurfaceState.Empty));
    }

    [Test]
    public void Contact_hover_prefers_exact_threat_range_over_unit_preview()
    {
        var range = new ThreatRangeAssessment(
            12_000,
            1_000,
            20_000,
            DlzState.OutOfZone,
            "DLZ: OutOfZone (Normal)",
            false);
        var preview = new EngagePreview("DLZ: InZone (Normal)", true, null);

        var result = DlzLiveSurfaceBinder.BindContact("c1", range, preview);

        Assert.That(result.DlzState, Is.EqualTo(DlzState.OutOfZone));
        Assert.That(result.ShortLabel, Is.EqualTo("OUT"));
    }

    [Test]
    public void Contact_hover_falls_back_to_unit_preview_when_no_threat_range()
    {
        var preview = new EngagePreview("DLZ: InZone (Normal)", true, null);

        var result = DlzLiveSurfaceBinder.BindContact("c1", null, preview);

        Assert.That(result.DlzState, Is.EqualTo(DlzState.InZone));
        Assert.That(result.ShortLabel, Is.EqualTo("IN"));
    }

    [Test]
    public void Contact_hover_without_range_or_preview_reports_unknown()
    {
        var result = DlzLiveSurfaceBinder.BindContact("c1", null, null);

        Assert.That(result.StateLine, Does.Contain("UNKNOWN"));
        Assert.That(result.DeclutterToken, Is.EqualTo(DlzDeclutterTokens.Unknown));
        Assert.That(result.CueClass, Is.EqualTo(DlzCueClasses.Unknown));
    }

    [Test]
    public void Panel_binder_maps_dlz_line_and_cue_class()
    {
        var state = DlzLiveSurfaceBinder.BindFromEngagePreview(
            new EngagePreview("DLZ: InZone (Normal)", true, null));
        var rows = DlzLiveSurfacePanelBinder.BindRows(state);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].ElementName, Is.EqualTo("dlz-line"));
        Assert.That(rows[0].Text, Is.EqualTo(state.StateLine));
        Assert.That(rows[0].CueClass, Is.EqualTo(DlzCueClasses.InZone));
    }

    [Test]
    public void Repeated_bind_is_replay_stable()
    {
        var range = new ThreatRangeAssessment(
            50_000,
            1_000,
            100_000,
            DlzState.InZone,
            "DLZ: InZone (Normal)",
            true);

        Assert.That(
            DlzLiveSurfaceBinder.Bind(range),
            Is.EqualTo(DlzLiveSurfaceBinder.Bind(range)));
    }

    [TestCase("DLZ: InZone (Normal)", DlzState.InZone)]
    [TestCase("DLZ: Approaching (Early)", DlzState.Approaching)]
    [TestCase("DLZ: OutOfZone (Late)", DlzState.OutOfZone)]
    [TestCase("DLZ: —", DlzState.Unknown)]
    public void BindFromEngagePreview_reads_projection_label_token(string label, DlzState expected)
    {
        var result = DlzLiveSurfaceBinder.BindFromEngagePreview(new EngagePreview(label, true, null));
        Assert.That(result.DlzState, Is.EqualTo(expected));
    }
}
