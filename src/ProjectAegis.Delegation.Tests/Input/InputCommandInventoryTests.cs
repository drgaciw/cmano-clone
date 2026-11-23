namespace ProjectAegis.Delegation.Tests.Input;

using NUnit.Framework;
using ProjectAegis.Delegation.Input;

[TestFixture]
public sealed class InputCommandInventoryTests
{
    [Test]
    public void Inventory_registers_drg247_and_drg272_commands_with_unique_ids_and_replay_gate()
    {
        var all = InputCommandInventory.All;
        Assert.That(all, Is.Not.Empty);

        var ids = all.Select(d => d.CommandId).ToArray();
        Assert.That(ids.Distinct(StringComparer.Ordinal), Has.Exactly(ids.Length).Items,
            "CommandIds in InputCommandInventory must be unique.");

        foreach (var required in new[]
        {
            AgentDirectiveIssuance.TakeControlId,
            AgentDirectiveIssuance.ReturnToAgentId,
            AgentDirectiveIssuance.PauseAgentId,
            AgentDirectiveIssuance.ResumeAgentId,
            InputCommandInventory.ContactReclassifyOverrideId,
            InputCommandInventory.ContactDropOverrideId,
        })
        {
            Assert.That(InputCommandInventory.TryGet(required, out var descriptor), Is.True, required);
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor!.PublicApiMethod, Is.Not.Null.And.Not.Empty);
            Assert.That(descriptor.BlockedByReplayViewer, Is.True);
            Assert.That(descriptor.AppendsDecisionLog, Is.True);
        }
    }

    [Test]
    public void Inventory_TryGet_matches_case_insensitively_and_rejects_unknown()
    {
        Assert.That(InputCommandInventory.TryGet(" PAUSE_AGENT ", out var pause), Is.True);
        Assert.That(pause!.Kind, Is.EqualTo(InputCommandJournalKind.PauseAgentActions));
        Assert.That(pause.PublicApiMethod, Does.Contain("TryPauseAgentActions"));

        Assert.That(InputCommandInventory.TryGet("not_a_command", out var missing), Is.False);
        Assert.That(missing, Is.Null);
        Assert.That(InputCommandInventory.TryGet(null, out _), Is.False);
    }
}
