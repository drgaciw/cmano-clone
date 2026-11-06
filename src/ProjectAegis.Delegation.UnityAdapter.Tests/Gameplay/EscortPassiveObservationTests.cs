namespace ProjectAegis.Delegation.UnityAdapter.Tests.Gameplay;

using NUnit.Framework;
using ProjectAegis.Delegation.Gameplay;

[TestFixture]
public sealed class EscortPassiveObservationTests
{
    [Test]
    public void Passive_track_does_not_reacquire_until_passive_observation_time_has_elapsed()
    {
        var definition = EscortMissionDefinition.LoadDefault();
        var world = new EscortMissionState(definition);
        world.SetRadar(true);
        for (var i = 0; i < 45 * 60; i++) world.Advance();
        Assert.That(world.HasFriendlyTrack, Is.True);
        world.SetRadar(false);
        for (var i = 0; i < 46 * 60; i++) world.Advance();
        Assert.That(world.ContactState, Is.EqualTo("Identified"), "Identity remains known after fire-control expires.");
        Assert.That(world.HasFriendlyTrack, Is.False, "Active-radar time must not count as passive observation.");
        for (var i = 0; i < 45 * 60; i++) world.Advance();
        Assert.That(world.HasFriendlyTrack, Is.True, "A full passive observation period reacquires the track.");
    }
}
