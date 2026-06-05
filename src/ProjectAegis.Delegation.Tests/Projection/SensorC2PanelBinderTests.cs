using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class SensorC2PanelBinderTests
{
    [Test]
    public void Bind_maps_emcon_track_and_contact_rows()
    {
        var snapshot = new SensorC2Snapshot(
            [
                new ContactPictureEntry("c1", "hostile-1", "u1", "Classified", 2, 2.0),
            ],
            ActiveContactCount: 1,
            ObserverRadarEmconActive: true,
            HasFireControlTrackOnPrimary: true,
            PrimaryTargetId: "hostile-1",
            ActiveEngagementCount: 0);

        var panel = SensorC2PanelBinder.Bind(snapshot);

        Assert.That(panel.EmconLabel, Is.EqualTo("EMCON: ACTIVE"));
        Assert.That(panel.TrackLabel, Is.EqualTo("TRACK: FC"));
        Assert.That(panel.ContactCountLabel, Is.EqualTo("CONTACTS: 1"));
        Assert.That(panel.ContactRows, Has.Count.EqualTo(1));
        Assert.That(panel.ContactRows[0].DisplayLine, Does.Contain("c1"));
        Assert.That(panel.ContactRows[0].DisplayLine, Does.Contain("Classified"));
    }

    [Test]
    public void Bind_empty_snapshot_shows_zero_contacts()
    {
        var panel = SensorC2PanelBinder.Bind(
            new SensorC2Snapshot(Array.Empty<ContactPictureEntry>(), 0, false, false, null, 0));

        Assert.That(panel.ContactCountLabel, Is.EqualTo("CONTACTS: 0"));
        Assert.That(panel.ContactRows, Is.Empty);
        Assert.That(panel.EmconLabel, Is.EqualTo("EMCON: OFF"));
    }
}