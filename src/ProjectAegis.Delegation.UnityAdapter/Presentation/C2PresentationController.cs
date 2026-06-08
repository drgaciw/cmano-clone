using System.Collections.Generic;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Presentation-only selection state (doc 20). Does not mutate sim or order log.</summary>
public sealed class C2PresentationController
{
    public string? SelectedUnitId { get; private set; }
    public string? SelectedContactId { get; private set; }
    public ContactSummaryEntry? SelectedContactSummary { get; private set; }

    public void SelectFriendlyUnit(string unitId)
    {
        SelectedUnitId = unitId;
        SelectedContactId = null;
        SelectedContactSummary = null;
    }

    public void SelectHostileContact(string contactId, IReadOnlyList<ContactPictureEntry> contacts)
    {
        SelectedContactId = contactId;
        SelectedContactSummary = ContactSummaryProjection.Project(contactId, contacts);
        SelectedUnitId = null;
    }

    public void ApplyDefaultSelection(IReadOnlyList<OobTreeEntry> oob)
    {
        if (!string.IsNullOrEmpty(SelectedUnitId) || !string.IsNullOrEmpty(SelectedContactId))
        {
            return;
        }

        var defaultUnit = C2SelectionResolver.ResolveDefaultFriendlyUnit(oob);
        if (defaultUnit != null)
        {
            SelectedUnitId = defaultUnit;
        }
    }

    public UnitDetailEntry? ResolveUnitDetail(
        ISimWorldSnapshot snapshot,
        TargetRegistry registry,
        DelegationBridge bridge)
    {
        if (string.IsNullOrEmpty(SelectedUnitId))
        {
            return UnitDetailBridge.BuildPrimary(snapshot, bridge);
        }

        return UnitDetailBridge.BuildSelected(
            new TargetId(SelectedUnitId),
            snapshot,
            bridge);
    }

    public string? ResolveContactLine() =>
        SelectedContactSummary?.DisplayLine;
}