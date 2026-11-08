// DRG-182: Authority / ROE / escalation chrome — binds C2AuthorityProjection + EscalationGate ledger.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AuthorityRoePanelHost : MonoBehaviour
    {
        private const string RootName = "authority-roe-root";
        private const string HeaderName = "authority-roe-header";
        private const string BadgeName = "authority-roe-badge";
        private const string RoeName = "authority-roe-roe";
        private const string TargetingName = "authority-roe-targeting";
        private const string VerbsName = "authority-roe-verbs";
        private const string GatesName = "authority-roe-gates";
        private const string NextName = "authority-roe-next";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _headerLine;
        private Label? _badgeLine;
        private Label? _roeLine;
        private Label? _targetingLine;
        private Label? _nextLine;
        private ListView? _verbList;
        private ListView? _gateList;
        private C2AuthorityPresentation _presentation = C2AuthorityPresentation.Empty;
        private bool _wired;
        private string? _lastContactId;
        private ulong _lastSimTick;

        /// <summary>Last projected authority presentation (headless-readable after Refresh).</summary>
        public C2AuthorityPresentation LastPresentation => _presentation;

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
            _headerLine = panel.Q<Label>(HeaderName);
            _badgeLine = panel.Q<Label>(BadgeName);
            _roeLine = panel.Q<Label>(RoeName);
            _targetingLine = panel.Q<Label>(TargetingName);
            _nextLine = panel.Q<Label>(NextName);
            _verbList = panel.Q<ListView>(VerbsName);
            _gateList = panel.Q<ListView>(GatesName);

            if (_verbList != null)
            {
                _verbList.makeItem = () => new Label();
                _verbList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _presentation.VerbRows.Count)
                    {
                        var row = _presentation.VerbRows[index];
                        label.text = C2AuthorityPresenter.FormatVerbLine(row);
                    }
                };
                _verbList.selectionType = SelectionType.None;
            }

            if (_gateList != null)
            {
                _gateList.makeItem = () => new Label();
                _gateList.bindItem = (element, index) =>
                {
                    if (element is Label label && index >= 0 && index < _presentation.GateLines.Count)
                    {
                        label.text = _presentation.GateLines[index];
                    }
                };
                _gateList.selectionType = SelectionType.None;
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _headerLine != null || _verbList != null;
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            var frame = bridgeHost.LastSliceAContacts;
            var contactId = bridgeHost.SelectedContactId;
            if (contactId == _lastContactId && frame.SimTick == _lastSimTick)
            {
                return;
            }

            _lastContactId = contactId;
            _lastSimTick = frame.SimTick;
            frame.Authorities.TryGetValue(contactId ?? string.Empty, out var authority);
            _presentation = C2AuthorityPresenter.Build(contactId, authority);
            ApplyPresentation();
        }

        /// <summary>Apply a projected authority state directly (headless / test path).</summary>
        public void Apply(C2AuthorityPresentation presentation)
        {
            _presentation = presentation ?? C2AuthorityPresentation.Empty;
            ApplyPresentation();
        }

        private void ApplyPresentation()
        {
            if (_headerLine != null)
            {
                _headerLine.text = _presentation.HeaderLine;
            }

            if (_badgeLine != null)
            {
                _badgeLine.text = _presentation.AdvisoryBadge;
            }

            if (_roeLine != null)
            {
                _roeLine.text = _presentation.RoeLine;
            }

            if (_targetingLine != null)
            {
                _targetingLine.text = _presentation.TargetingLine;
            }

            if (_nextLine != null)
            {
                _nextLine.text = _presentation.NextActionLine;
            }

            if (_verbList != null)
            {
                _verbList.itemsSource = (System.Collections.IList)_presentation.VerbRows;
                _verbList.Rebuild();
            }

            if (_gateList != null)
            {
                _gateList.itemsSource = (System.Collections.IList)_presentation.GateLines;
                _gateList.Rebuild();
            }

            var rootEl = _document.rootVisualElement?.Q(RootName);
            if (rootEl != null)
            {
                rootEl.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

    }
}
#endif
