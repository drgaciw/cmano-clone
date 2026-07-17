// ASSET-021 Combat Domains Hot-Tick HUD — UI Toolkit host bound to headless binder + tracker (S105 A1).
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class CombatDomainsHotTickHost : MonoBehaviour
    {
        private const string RootName = "combat-domains-hot-tick-root";

        [SerializeField] private DelegationBridgeHost? bridgeHost;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _panelRoot;
        private readonly Dictionary<string, Label> _stateLabels = new();
        private readonly CombatDomainsHotTickTracker _tracker = new();
        private CombatDomainsHotTickPanelState _panelState = CombatDomainsHotTickPanelBinder.BindIdle();
        private bool _wired;

        /// <summary>Last bound panel state (headless binder output).</summary>
        public CombatDomainsHotTickPanelState PanelState => _panelState;

        /// <summary>Headless tracker used for domain engagement (tests / offline).</summary>
        public CombatDomainsHotTickTracker Tracker => _tracker;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (panelAsset != null)
            {
                _document.visualTreeAsset = panelAsset;
            }

            if (panelStyles != null && _document.rootVisualElement != null)
            {
                _document.rootVisualElement.styleSheets.Add(panelStyles);
            }
        }

        private void OnEnable()
        {
            TryWireElements();
            RefreshFromBridge();
        }

        private void LateUpdate()
        {
            if (!showPanel)
            {
                return;
            }

            if (!_wired)
            {
                TryWireElements();
            }

            RefreshFromBridge();
        }

        /// <summary>Apply binder state directly (tests / offline preview).</summary>
        public void ApplyPanelState(CombatDomainsHotTickPanelState state)
        {
            _panelState = state ?? CombatDomainsHotTickPanelBinder.BindIdle();
            ApplyStateToLabels();
        }

        /// <summary>Apply tracker snapshot through the headless binder.</summary>
        public void ApplyTracker(CombatDomainsHotTickTracker tracker)
        {
            if (tracker == null)
            {
                _panelState = CombatDomainsHotTickPanelBinder.BindIdle();
            }
            else
            {
                _panelState = CombatDomainsHotTickPanelBinder.BindFromTracker(tracker);
            }

            ApplyStateToLabels();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            _panelRoot = root.Q<VisualElement>(RootName) ?? root;
            if (panelStyles != null && !_panelRoot.styleSheets.Contains(panelStyles))
            {
                _panelRoot.styleSheets.Add(panelStyles);
            }

            _stateLabels.Clear();
            TryMapLabel("domain-air-state", "Air");
            TryMapLabel("domain-surface-state", "Surface");
            TryMapLabel("domain-sub-state", "Subsurface");
            TryMapLabel("domain-land-state", "Land");
            TryMapLabel("domain-mine-state", "Mine");
            TryMapLabel("domain-facility-state", "Facility");

            _wired = _stateLabels.Count > 0;
            _panelRoot.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            ApplyStateToLabels();
        }

        private void TryMapLabel(string elementName, string domainKey)
        {
            if (_panelRoot == null)
            {
                return;
            }

            var label = _panelRoot.Q<Label>(elementName);
            if (label != null)
            {
                _stateLabels[domainKey] = label;
            }
        }

        private void RefreshFromBridge()
        {
            if (!_wired)
            {
                return;
            }

            // Tracker rebuild each refresh from bridge message log + activity tags.
            // No sim hot-path coupling; Degraded (POLICY) requires message-log observation.
            _tracker.Clear();
            if (bridgeHost != null)
            {
                // Stamp with message-log depth (stable, headless-safe; not wall-clock).
                var tick = bridgeHost.LastMessageLog?.Count ?? 0;
                _tracker.ObserveMessageLog(tick, bridgeHost.LastMessageLog);
                _tracker.ObserveActiveDomainTags(tick, bridgeHost.LastCombatDomainActivityTags);
            }

            _panelState = CombatDomainsHotTickPanelBinder.BindFromTracker(_tracker);
            ApplyStateToLabels();
        }

        private void ApplyStateToLabels()
        {
            foreach (var row in _panelState.Rows)
            {
                if (!_stateLabels.TryGetValue(row.DomainKey, out var label))
                {
                    continue;
                }

                label.text = row.StateLabel;
                label.ClearClassList();
                label.AddToClassList("combat-domains-hot-tick__state");
                if (!string.IsNullOrEmpty(row.StateCssClass)
                    && row.StateCssClass != "combat-domains-hot-tick__state")
                {
                    label.AddToClassList(row.StateCssClass);
                }
            }
        }
    }
}
#endif
