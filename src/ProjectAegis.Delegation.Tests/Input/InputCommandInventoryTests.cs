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

    [Test]
    public void Inventory_covers_all_16_contract_commands_adr029_overrides_and_play_delegation_commands()
    {
        var all = InputCommandInventory.All;
        var allKinds = Enum.GetValues<InputCommandJournalKind>();

        Assert.That(allKinds, Has.Length.EqualTo(22));
        Assert.That(all, Has.Count.EqualTo(22));
        Assert.That(all.Select(d => d.Kind).Distinct(), Has.Exactly(22).Items);
        Assert.That(all.Select(d => d.CommandId).Distinct(StringComparer.Ordinal), Has.Exactly(22).Items);

        foreach (var kind in allKinds)
        {
            Assert.That(all.Any(d => d.Kind == kind), Is.True, $"Missing inventory entry for {kind}");
        }

        foreach (var required in new[]
        {
            AgentDirectiveIssuance.TakeControlId,
            AgentDirectiveIssuance.ReturnToAgentId,
            AgentDirectiveIssuance.PauseAgentId,
            AgentDirectiveIssuance.ResumeAgentId,
            InputCommandInventory.EnqueueHumanOrderId,
            InputCommandInventory.IssuePlayerCommandId,
            InputCommandInventory.EnqueueAttackOptionId,
            InputCommandInventory.ApprovePendingOrderId,
            InputCommandInventory.RejectPendingOrderId,
            InputCommandInventory.RebindAgentTraitsId,
            InputCommandInventory.PauseSimId,
            InputCommandInventory.ResumeSimId,
            InputCommandInventory.SetTimeAccelerationId,
            InputCommandInventory.ContactReclassifyOverrideId,
            InputCommandInventory.ContactDropOverrideId,
            InputCommandInventory.RebriefAgentId,
            InputCommandInventory.AssignAgentControllerId,
            InputCommandInventory.DoctrineOverrideId,
            InputCommandInventory.AcknowledgeWatchAttentionId,
            InputCommandInventory.DismissWatchAttentionId,
            InputCommandInventory.SetAgentPersonalityId,
            InputCommandInventory.SetAgentAutonomyId,
        })
        {
            Assert.That(InputCommandInventory.TryGet(required, out var descriptor), Is.True, required);
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor!.PublicApiMethod, Is.Not.Null.And.Not.Empty, required);
            Assert.That(descriptor.BlockedByReplayViewer, Is.True, required);
        }
    }

    [Test]
    public void Inventory_marks_state_only_vs_log_appending_commands_accurately()
    {
        var stateOnlyKinds = new HashSet<InputCommandJournalKind>
        {
            InputCommandJournalKind.ApprovePendingOrder,
            InputCommandJournalKind.RejectPendingOrder,
            InputCommandJournalKind.RebindAgentTraits,
            InputCommandJournalKind.PauseSim,
            InputCommandJournalKind.ResumeSim,
            InputCommandJournalKind.SetTimeAccelerationFactor,
            InputCommandJournalKind.AcknowledgeWatchAttention,
            InputCommandJournalKind.DismissWatchAttention,
        };

        foreach (var entry in InputCommandInventory.All)
        {
            var expectedAppendsLog = !stateOnlyKinds.Contains(entry.Kind);
            Assert.That(
                entry.AppendsDecisionLog,
                Is.EqualTo(expectedAppendsLog),
                $"AppendsDecisionLog mismatch for {entry.Kind} ({entry.CommandId}).");
        }
    }
}
