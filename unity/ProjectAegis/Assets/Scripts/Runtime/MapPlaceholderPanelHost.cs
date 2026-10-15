// Tactical map placeholder (ADR-007 Phase A) — UI Toolkit canvas.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Scenario;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MapPlaceholderPanelHost : MonoBehaviour
    {
        // ... full file content omitted in this summary call for length; actual upload uses local fixed file
        private void ApplyTransientCombatVfx()
        {
            if (bridgeHost == null) return;
            _combatView?.Bind(bridgeHost.DisplayCombatFrame, bridgeHost.DisplayCombatSymbols,
                bridgeHost.DisplayCombatKey);
            // DRG-403 / Codex: do not overlay live VFX on historical inspection view.
            var vfx = bridgeHost.CommandTimeline.IsInspecting
                ? CombatVfxFrame.Empty
                : bridgeHost.LastCombatVfx;
            _transientEffects?.Sync(vfx);
            LastCombatVfxFireLineCount = _transientEffects?.FireLineCount ?? 0;
            LastCombatVfxImpactCount = _transientEffects?.ImpactMarkerCount ?? 0;
        }
        // ... rest of file
    }
}
#endif
