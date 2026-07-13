using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0 T6: edit-mode toggle → Mission Board entry intent (string IntentKind only).</summary>
[TestFixture]
public sealed class MissionBoardEntryResolverTests
{
    [Test]
    public void TryToggle_from_play_enters_mission_board()
    {
        var intent = MissionBoardEntryResolver.TryToggle(currentlyInEditMode: false, isReplay: false, out var failure);

        Assert.That(failure, Is.Null);
        Assert.That(intent, Is.Not.Null);
        Assert.That(intent!.EnterEditMode, Is.True);
        Assert.That(intent.IntentKind, Is.EqualTo(MissionBoardEntryResolver.EnterIntentKind));
    }

    [Test]
    public void TryToggle_from_edit_exits_mission_board()
    {
        var intent = MissionBoardEntryResolver.TryToggle(currentlyInEditMode: true, isReplay: false, out var failure);

        Assert.That(failure, Is.Null);
        Assert.That(intent, Is.Not.Null);
        Assert.That(intent!.EnterEditMode, Is.False);
        Assert.That(intent.IntentKind, Is.EqualTo(MissionBoardEntryResolver.ExitIntentKind));
    }

    [Test]
    public void TryToggle_replay_blocks()
    {
        var intent = MissionBoardEntryResolver.TryToggle(currentlyInEditMode: false, isReplay: true, out var failure);

        Assert.That(intent, Is.Null);
        Assert.That(failure, Is.EqualTo(MissionBoardEntryResolver.FailureReplay));
    }
}
