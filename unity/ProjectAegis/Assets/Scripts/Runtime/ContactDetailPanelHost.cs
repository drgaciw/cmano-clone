// CMD-29 contact detail panel — belief/inference view distinct from own-unit RightUnitPanelHost.
#if UNITY_5_3_OR_NEWER
using System.Linq;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ContactDetailPanelHost : MonoBehaviour
    {
        private const string RootName = "contact-detail-root";
        private const string ContactIdName = "contact-id-line";
        private const string TargetIdName = "target-id-line";
        private const string LifecycleName = "lifecycle-line";
        private const string ClassificationName = "classification-line";
        private const string ConfidenceName = "confidence-line";
        private const string ProvenanceName = "provenance-line";
        private const string WraName = "wra-line";
        private const string BdaName = "bda-line";
        private const string StalenessName = "staleness-line";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _contactIdLine;
        private Label? _targetIdLine;
        private Label? _lifecycleLine;
        private Label? _classificationLine;
        private Label? _confidenceLine;
        private Label? _provenanceLine;
        private Label? _wraLine;
        private Label? _bdaLine;
        private Label? _stalenessLine;
        private bool _wired;
        private ContactDetailPresentation _presentation = ContactDetailPresentation.Empty;

        /// <summary>Last applied contact-detail presentation (CMD-29).</summary>
        public ContactDetailPresentation LastPresentation => _presentation;

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
            _contactIdLine = panel.Q<Label>(ContactIdName);
            _targetIdLine = panel.Q<Label>(TargetIdName);
            _lifecycleLine = panel.Q<Label>(LifecycleName);
            _classificationLine = panel.Q<Label>(ClassificationName);
            _confidenceLine = panel.Q<Label>(ConfidenceName);
            _provenanceLine = panel.Q<Label>(ProvenanceName);
            _wraLine = panel.Q<Label>(WraName);
            _bdaLine = panel.Q<Label>(BdaName);
            _stalenessLine = panel.Q<Label>(StalenessName);
            _wired = _contactIdLine != null && _targetIdLine != null && _classificationLine != null;

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }
        }

        /// <summary>Apply presentation via headless apply-state (tests / direct bind).</summary>
        public void ApplyPresentation(ContactDetailPresentation presentation)
        {
            _presentation = presentation ?? ContactDetailPresentation.Empty;
            ApplyPresentationToLabels();
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                return;
            }

            var contactId = bridgeHost.SelectedContactId;
            if (string.IsNullOrEmpty(contactId))
            {
                _presentation = ContactDetailPresentation.Empty;
            }
            else
            {
                var contacts = bridgeHost.LastSensorC2.Contacts;
                if (contacts == null || contacts.Count == 0)
                {
                    contacts = ContactPictureProjection.Project(
                        bridgeHost.Bridge.Orchestrator.DecisionLog);
                }

                var currentSimTick = ResolveCurrentSimTick(contacts);
                _presentation = ContactDetailApplyState.ProjectAndApply(
                    contactId,
                    contacts,
                    currentSimTick);
            }

            ApplyPresentationToLabels();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                // Show when a contact is selected (or always if showPanel); hide when unit-only selection preferred.
                var hasContact = !string.IsNullOrEmpty(bridgeHost.SelectedContactId);
                root.style.display = showPanel && hasContact ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static ulong ResolveCurrentSimTick(System.Collections.Generic.IReadOnlyList<ContactPictureEntry> contacts)
        {
            if (contacts == null || contacts.Count == 0)
            {
                return 0UL;
            }

            // Prefer freshest picture tick as "now" when host has no live snapshot clock.
            return contacts.Max(c => c.LastSimTick);
        }

        private void ApplyPresentationToLabels()
        {
            if (_contactIdLine != null)
            {
                _contactIdLine.text = _presentation.ContactIdLine;
            }

            if (_targetIdLine != null)
            {
                _targetIdLine.text = _presentation.TargetIdLine;
            }

            if (_lifecycleLine != null)
            {
                _lifecycleLine.text = _presentation.LifecycleLine;
            }

            if (_classificationLine != null)
            {
                _classificationLine.text = _presentation.ClassificationLine;
            }

            if (_confidenceLine != null)
            {
                _confidenceLine.text = _presentation.ConfidenceLine;
            }

            if (_provenanceLine != null)
            {
                _provenanceLine.text = _presentation.DetectionProvenanceLine;
            }

            if (_wraLine != null)
            {
                _wraLine.text = _presentation.WraLine;
            }

            if (_bdaLine != null)
            {
                _bdaLine.text = _presentation.BdaLine;
            }

            if (_stalenessLine != null)
            {
                _stalenessLine.text = _presentation.StalenessLine;
            }
        }
    }
}
#endif
