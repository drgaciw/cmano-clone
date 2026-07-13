// Doctrine inheritance panel (req 13 P0) — binds ResolvedUnitPolicy projection to UI Toolkit.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class DoctrineInheritancePanelHost : MonoBehaviour
    {
        private const string RootName = "doctrine-root";
        private const string UnitIdName = "unit-id-label";
        private const string RoeName = "roe-label";
        private const string SalvoName = "salvo-label";
        private const string EmconName = "emcon-label";
        private const string SourceName = "source-label";
        private const string OverrideName = "override-label";
        private const string RoeDropdownName = "roe-dropdown";
        private const string ApplyButtonName = "apply-override-button";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _unitIdLabel;
        private Label? _roeLabel;
        private Label? _salvoLabel;
        private Label? _emconLabel;
        private Label? _sourceLabel;
        private Label? _overrideLabel;
        private DropdownField? _roeDropdown;
        private Button? _applyButton;
        private DoctrineInheritancePanelState _panelState = new(
            "UNIT: —", "ROE: —", "SALVO: —", "EMCON: —", "SOURCE: —", "OVERRIDE: UNAVAILABLE",
            false, System.Array.Empty<RoeLevelOption>());
        private bool _wired;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }

            _document = GetComponent<UIDocument>();
        }

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
            Refresh();
        }

        private void LateUpdate()
        {
            if (!showPanel || bridgeHost == null)
            {
                return;
            }

            if (!_wired)
            {
                TryWireElements();
            }

            Refresh();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _unitIdLabel = panel.Q<Label>(UnitIdName);
            _roeLabel = panel.Q<Label>(RoeName);
            _salvoLabel = panel.Q<Label>(SalvoName);
            _emconLabel = panel.Q<Label>(EmconName);
            _sourceLabel = panel.Q<Label>(SourceName);
            _overrideLabel = panel.Q<Label>(OverrideName);
            _roeDropdown = panel.Q<DropdownField>(RoeDropdownName);
            _applyButton = panel.Q<Button>(ApplyButtonName);
            _wired = _unitIdLabel != null && _roeLabel != null && _salvoLabel != null &&
                     _emconLabel != null && _sourceLabel != null && _overrideLabel != null &&
                     _roeDropdown != null && _applyButton != null;

            if (_applyButton != null)
            {
                _applyButton.clicked -= OnApplyOverrideClicked;
                _applyButton.clicked += OnApplyOverrideClicked;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            bridgeHost.RefreshDoctrineInheritance();
            var entry = bridgeHost.LastDoctrineInheritance;
            _panelState = DoctrineInheritancePanelBinder.Bind(entry);

            _unitIdLabel!.text = _panelState.UnitIdLine;
            _roeLabel!.text = _panelState.RoeLine;
            _salvoLabel!.text = _panelState.SalvoLine;
            _emconLabel!.text = _panelState.EmconLine;
            _sourceLabel!.text = _panelState.SourceLine;
            _overrideLabel!.text = _panelState.OverrideLine;

            if (_roeDropdown != null)
            {
                RefreshDropdown();
            }

            if (_applyButton != null)
            {
                _applyButton.SetEnabled(_panelState.CanOverride);
            }

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void RefreshDropdown()
        {
            if (_roeDropdown == null || _panelState.RoeOptions.Count == 0)
            {
                return;
            }

            var choices = new List<string>();
            foreach (var option in _panelState.RoeOptions)
            {
                choices.Add(option.Label);
            }

            _roeDropdown.choices = choices;

            var current = _panelState.RoeOptions.FirstOrDefault(o => o.IsCurrent);
            if (current != null)
            {
                _roeDropdown.value = current.Label;
            }
        }

        private void OnApplyOverrideClicked()
        {
            if (bridgeHost == null || _roeDropdown == null || string.IsNullOrEmpty(_roeDropdown.value))
            {
                return;
            }

            if (bridgeHost.TrySetDoctrineOverride(_roeDropdown.value))
            {
                Refresh();
            }
        }
    }
}
#endif
