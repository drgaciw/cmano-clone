// CMD-24 / DRG-261 magazine loadout depth — weapon remaining/capacity + armable airframe feasibility.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MagazineLoadoutPanelHost : MonoBehaviour
    {
        private const string RootName = "magazine-loadout-root";
        private const string HeaderName = "magazine-loadout-header";
        private const string EmptyName = "magazine-loadout-empty";
        private const string ListName = "magazine-loadout-list";
        private const string FeasibilityName = "magazine-loadout-feasibility";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private int roundsPerAirframe = 6;

        private UIDocument _document = null!;
        private Label? _headerLine;
        private Label? _emptyLine;
        private Label? _feasibilityLine;
        private ListView? _list;
        private MagazineLoadoutPresentation _presentation = MagazineLoadoutPresentation.Empty;
        private MagazineLoadoutPanelLabels _labels = new(
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<MagazineLoadoutRowLabels>(),
            "ml:empty",
            true);
        private string? _lastFingerprint;
        private bool _wired;

        /// <summary>Last applied magazine presentation (CMD-24 / DRG-261).</summary>
        public MagazineLoadoutPresentation LastPresentation => _presentation;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }

            _document = GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
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
            _wired = false;
            _lastFingerprint = null;
            TryWireElements();
            Refresh(force: true);
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

            bridgeHost.RefreshMagazineLoadout();
            Refresh(force: false);
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _headerLine = panel.Q<Label>(HeaderName);
            _emptyLine = panel.Q<Label>(EmptyName);
            _feasibilityLine = panel.Q<Label>(FeasibilityName);
            _list = panel.Q<ListView>(ListName);

            if (_list != null)
            {
                _list.makeItem = () => new Label();
                _list.bindItem = (element, index) =>
                {
                    if (element is not Label label || index < 0 || index >= _labels.Rows.Count)
                    {
                        return;
                    }

                    var row = _labels.Rows[index];
                    label.text = row.DisplayLine;
                    label.tooltip = $"{row.Remaining}/{row.Capacity} ({row.FillPct:0}%) [{row.StatusLine}]";
                };
                _list.selectionType = SelectionType.None;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _headerLine != null || _list != null;
        }

        /// <summary>Apply presentation via headless apply-state (tests / direct bind).</summary>
        public void ApplyPresentation(MagazineLoadoutPresentation presentation)
        {
            _presentation = presentation ?? MagazineLoadoutPresentation.Empty;
            var labels = MagazineLoadoutPanelBinder.Bind(_presentation, roundsPerAirframe);
            _lastFingerprint = labels.Fingerprint;
            ApplyBoundLabels(labels);
        }

        private void Refresh(bool force)
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            _presentation = MagazineLoadoutPresenter.Build(
                bridgeHost.LastMagazineLoadout,
                bridgeHost.HasMagazineLoadoutData);
            var labels = MagazineLoadoutPanelBinder.Bind(_presentation, roundsPerAirframe);
            if (!force
                && string.Equals(labels.Fingerprint, _lastFingerprint, StringComparison.Ordinal))
            {
                return;
            }

            _lastFingerprint = labels.Fingerprint;
            ApplyBoundLabels(labels);

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplyBoundLabels(MagazineLoadoutPanelLabels labels)
        {
            _labels = labels;

            if (_headerLine != null)
            {
                _headerLine.text = labels.HeaderLine;
            }

            if (_emptyLine != null)
            {
                _emptyLine.text = labels.EmptyStateLine;
                _emptyLine.style.display = !labels.HasMagazineData
                    || !string.IsNullOrEmpty(labels.EmptyStateLine)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_list != null)
            {
                _list.style.display = labels.HasMagazineData && labels.Rows.Count > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _list.itemsSource = labels.Rows as System.Collections.IList
                    ?? new List<MagazineLoadoutRowLabels>(labels.Rows);
                _list.Rebuild();
            }

            if (_feasibilityLine != null)
            {
                _feasibilityLine.text = labels.FeasibilityLine;
            }
        }
    }
}
#endif
