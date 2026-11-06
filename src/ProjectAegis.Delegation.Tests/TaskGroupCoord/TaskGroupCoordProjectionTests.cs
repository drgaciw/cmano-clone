using ProjectAegis.Delegation.TaskGroupCoord;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.TaskGroupCoord;

public sealed class TaskGroupCoordProjectionTests
{
    private static TaskGroupCoordInput CompleteGroup() =>
        new(
            GroupId: "tg-alpha",
            Members: new[] { "2002", "2001", "2003" },
            PackageId: "pkg-patrol-alpha",
            PackageLabel: "Patrol Package Alpha",
            HasC2: true,
            C2NodeId: "c2-node-1",
            IsSplit: false);

    [Test]
    public void Complete_group_reports_gap_none_with_stable_fingerprint()
    {
        var input = CompleteGroup();
        var snapshot = TaskGroupCoordProjection.Project(input);

        Assert.That(snapshot.Kind, Is.EqualTo(TaskGroupCoordKind.AdvisoryCoordination));
        Assert.That(snapshot.GapCode, Is.EqualTo(TaskGroupCoordGapCode.None));
        Assert.That(snapshot.GroupId, Is.EqualTo("tg-alpha"));
        Assert.That(snapshot.Members, Is.EqualTo(new[] { "2001", "2002", "2003" }));
        Assert.That(snapshot.AssignedPackageId, Is.EqualTo("pkg-patrol-alpha"));
        Assert.That(snapshot.AssignedPackageLabel, Is.EqualTo("Patrol Package Alpha"));
        Assert.That(snapshot.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.IsAutomaticEngagement, Is.False);
        Assert.That(snapshot.StatusLine, Does.Contain("COORD OK").IgnoreCase);
        Assert.That(snapshot.StatusLine, Does.Contain("no orders").IgnoreCase);

        var rerun = TaskGroupCoordProjection.Project(input);
        Assert.That(
            TaskGroupCoordProjection.ComputeFingerprint(snapshot),
            Is.EqualTo(TaskGroupCoordProjection.ComputeFingerprint(rerun)));
    }

    [Test]
    public void Split_group_reports_split_gap_even_when_other_facts_missing()
    {
        var input = new TaskGroupCoordInput(
            GroupId: "tg-bravo",
            Members: new[] { "3001" },
            PackageId: string.Empty,
            PackageLabel: string.Empty,
            HasC2: false,
            IsSplit: true);

        var snapshot = TaskGroupCoordProjection.Project(input);

        Assert.That(snapshot.GapCode, Is.EqualTo(TaskGroupCoordGapCode.Split));
        Assert.That(snapshot.StatusLine, Does.Contain("GAP SPLIT").IgnoreCase);
        Assert.That(snapshot.IsFireOrder, Is.False);
    }

    [Test]
    public void Missing_c2_reports_no_c2_gap_when_not_split()
    {
        var input = new TaskGroupCoordInput(
            GroupId: "tg-charlie",
            Members: new[] { "4001", "4002" },
            PackageId: "pkg-strike-one",
            PackageLabel: "Strike Package One",
            HasC2: false,
            IsSplit: false);

        var snapshot = TaskGroupCoordProjection.Project(input);

        Assert.That(snapshot.GapCode, Is.EqualTo(TaskGroupCoordGapCode.NoC2));
        Assert.That(snapshot.StatusLine, Does.Contain("GAP NO C2").IgnoreCase);
        Assert.That(snapshot.IsWeaponsReleaseAuthorization, Is.False);
    }

    [Test]
    public void Unassigned_package_reports_unassigned_gap_when_c2_present()
    {
        var input = new TaskGroupCoordInput(
            GroupId: "tg-delta",
            Members: new[] { "5001" },
            PackageId: string.Empty,
            PackageLabel: string.Empty,
            HasC2: true,
            C2NodeId: "c2-node-2",
            IsSplit: false);

        var snapshot = TaskGroupCoordProjection.Project(input);

        Assert.That(snapshot.GapCode, Is.EqualTo(TaskGroupCoordGapCode.Unassigned));
        Assert.That(snapshot.StatusLine, Does.Contain("GAP UNASSIGNED").IgnoreCase);
        Assert.That(snapshot.IsAutomaticEngagement, Is.False);
    }

    [Test]
    public void Empty_input_returns_empty_snapshot()
    {
        var nullSnapshot = TaskGroupCoordProjection.Project(null);
        Assert.That(nullSnapshot.GroupId, Is.Empty);
        Assert.That(nullSnapshot.Members, Is.Empty);
        Assert.That(TaskGroupCoordProjection.ComputeFingerprint(nullSnapshot), Is.EqualTo("tgc:empty"));

        var emptyGroupId = TaskGroupCoordProjection.Project(
            new TaskGroupCoordInput(
                GroupId: string.Empty,
                Members: Array.Empty<string>(),
                PackageId: string.Empty,
                PackageLabel: string.Empty,
                HasC2: false));

        Assert.That(emptyGroupId.GroupId, Is.Empty);
        Assert.That(TaskGroupCoordProjection.ComputeFingerprint(emptyGroupId), Is.EqualTo("tgc:empty"));
    }
}
