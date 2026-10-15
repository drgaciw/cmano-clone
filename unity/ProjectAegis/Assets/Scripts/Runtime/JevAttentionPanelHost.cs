// DRG-319 Jev VS1 attention Score + skill-unavailable disclosure. Chrome only.
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    /// <summary>
    /// Advisory attention list. Reads a projection JSON string and paints rows.
    /// Does not enqueue player commands (ADR-010).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class JevAttentionPanelHost : MonoBehaviour
    {
        private const string RootName = "jev-attention-root";
        private const string DisclosureName = "jev-disclosure-banner";
        private const string ModelPinName = "jev-model-pin";
        private const string ListName = "jev-attention-list";

        [SerializeField] private UIDocument? documentOverride;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _disclosure;
        private Label? _modelPin;
        private VisualElement? _list;
        private bool _wired;

        /// <summary>Last applied advisory panel.</summary>
        public JevAttentionPanel? LastPanel { get; private set; }

        private void Awake()
        {
            _document = documentOverride != null ? documentOverride : GetComponent<UIDocument>();
            UiDocumentPanelSettingsBootstrap.EnsureDocument(_document);
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
            TryWire();
            if (LastPanel != null)
            {
                Apply(LastPanel);
            }
        }

        /// <summary>Paint a panel already projected by <see cref="JevAttentionBinder"/>.</summary>
        public void Apply(JevAttentionPanel panel)
        {
            LastPanel = panel ?? throw new System.ArgumentNullException(nameof(panel));
            TryWire();
            if (!_wired)
            {
                return;
            }

            ApplyWired(panel);
        }

        /// <summary>Project synthetic state JSON and paint it. Refuses non-VS1 schemas.</summary>
        public void ApplyStateJson(string json)
        {
            Apply(JevAttentionBinder.Project(json));
        }

        private void TryWire()
        {
            _wired = false;
            _disclosure = null;
            _modelPin = null;
            _list = null;
            var root = _document != null && _document.isActiveAndEnabled
                ? _document.rootVisualElement
                : null;
            if (root == null)
            {
                return;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            _disclosure = panel.Q<Label>(DisclosureName);
            _modelPin = panel.Q<Label>(ModelPinName);
            _list = panel.Q<VisualElement>(ListName);
            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _disclosure != null && _modelPin != null && _list != null;
        }

        private void ApplyWired(JevAttentionPanel panel)
        {
            if (_disclosure != null)
            {
                var disclosure = panel.Disclosure;
                _disclosure.text = disclosure.BannerText;
                _disclosure.style.display = disclosure.Visible ? DisplayStyle.Flex : DisplayStyle.None;
                _disclosure.EnableInClassList(JevDisclosurePresentation.VisibleCssClass, disclosure.Visible);
                _disclosure.EnableInClassList(JevDisclosurePresentation.HiddenCssClass, !disclosure.Visible);
            }

            if (_modelPin != null)
            {
                _modelPin.text = panel.ScoreSourceLabel;
            }

            if (_list != null)
            {
                _list.Clear();
                for (var i = 0; i < panel.Rows.Count; i++)
                {
                    var row = panel.Rows[i];
                    var label = new Label(row.Line)
                    {
                        name = "jev-attention-row",
                    };
                    label.AddToClassList("jev-attention-row");
                    label.AddToClassList(row.CssClass);
                    _list.Add(label);
                }
            }

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
#endif
