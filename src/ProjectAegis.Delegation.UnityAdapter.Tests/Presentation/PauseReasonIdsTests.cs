using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>Req 20 P0 Phase 0: canonical pause-reason ids work with PauseReasonStack.</summary>
[TestFixture]
public sealed class PauseReasonIdsTests
{
    [Test]
    public void Stack_accepts_canonical_ids_and_runs_only_when_empty()
    {
        var stack = new PauseReasonStack();
        Assert.That(stack.IsPaused, Is.False);

        Assert.That(stack.Push(PauseReasonIds.User), Is.True);
        Assert.That(stack.IsPaused, Is.True);

        Assert.That(stack.Push(PauseReasonIds.AutoPauseSeverity), Is.True);
        Assert.That(stack.Push(PauseReasonIds.AgentGate), Is.True);
        Assert.That(stack.Push(PauseReasonIds.MultitaskerBookmark), Is.True);

        Assert.That(stack.Push(PauseReasonIds.User), Is.False, "duplicate user reason is no-op");

        Assert.That(stack.Remove(PauseReasonIds.User), Is.True);
        Assert.That(stack.Remove(PauseReasonIds.AutoPauseSeverity), Is.True);
        Assert.That(stack.Remove(PauseReasonIds.AgentGate), Is.True);
        Assert.That(stack.Remove(PauseReasonIds.MultitaskerBookmark), Is.True);
        Assert.That(stack.IsPaused, Is.False);
    }
}
