using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class ContactSummaryProjectionTests
{
    [Test]
    public void Project_returns_display_line_for_known_contact()
    {
        var summary = ContactSummaryProjection.Project(
            "c1",
            [new ContactPictureEntry("c1", "t1", "obs-1", "CLASSIFIED", 100ul, 0.9)]);

        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.ContactId, Is.EqualTo("c1"));
        Assert.That(summary.DisplayLine, Does.Contain("CLASSIFIED"));
    }
}