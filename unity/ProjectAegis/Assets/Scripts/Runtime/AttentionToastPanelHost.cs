// CMD-39 Track A — attention toast host. Binds WatchAutoPauseGate + AttentionTierAlertProjection.
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AttentionToastPanelHost : MonoBehaviour
    {
        private const string RootName = "attention-toast-root";
        private const string CardName = "attention-toast-card";
        private const string SeverityName = "attention-toast-severity";
        private const string TitleName = "attention-toast-title";
        private const string BodyName = "attention-toast-body";
        private const string QueueName = "attention-toast-queue";
        private const string AckName = "attention-toast-ack";
        private const string DismissName = "attention-toast-dismiss";

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [Tooltip("Seed one pause-class watch event so Play Mode shows a toast + auto-pause.")]
        [SerializeField] private bool seedDemoWatchOnStart = true;

        private UIDocument _document = null!;
        private VisualElement? _root;
        private VisualElement? _card;
        private Label? _severity;
        private Label? _title;
        private Label? _body;
        private Label? _queue;
        private Button? _ack;
        private Button? _dismiss;
        private bool _wired;
        private bool _demoSeeded;
        private AttentionToastPresentation _presentation = AttentionToastPresentation.Empty;

        /// <summary>Last applied toast presentation (headless-readable after Refresh).</summary>
        public AttentionToastPresentation LastPresentation => _presentation;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
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
            TryWireElements();
            Refresh();
        }

        private void Start()
        {
            TrySeedDemoWatch();
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

            TrySeedDemoWatch();
            Refresh();
        }

        private void TryWireElements()
        {
            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            _root = root.Q<VisualElement>(RootName) ?? root;
            _card = _root.Q<VisualElement>(CardName);
            _severity = _root.Q<Label>(SeverityName);
            _title = _root.Q<Label>(TitleName);
            _body = _root.Q<Label>(BodyName);
            _queue = _root.Q<Label>(QueueName);
            _ack = _root.Q<Button>(AckName);
            _dismiss = _root.Q<Button>(DismissName);

            if (panelStyles != null && !_root.styleSheets.Contains(panelStyles))
            {
                _root.styleSheets.Add(panelStyles);
            }

            if (_ack != null)
            {
                _ack.clicked -= OnAckClicked;
                _ack.clicked += OnAckClicked;
            }

            if (_dismiss != null)
            {
                _dismiss.clicked -= OnDismissClicked;
                _dismiss.clicked += OnDismissClicked;
            }

            _wired = _title != null && _body != null && _ack != null;
        }

        private void TrySeedDemoWatch()
        {
            if (_demoSeeded || !seedDemoWatchOnStart || bridgeHost == null)
            {
                return;
            }

            _demoSeeded = bridgeHost.TrySeedDemoWatchAttention();
        }

        private void OnAckClicked()
        {
            if (bridgeHost == null || _presentation.Active == null)
            {
                return;
            }

            bridgeHost.TryAcknowledgeAttentionToast(_presentation.Active.CardId);
            Refresh();
        }

        private void OnDismissClicked()
        {
            if (bridgeHost == null || _presentation.Active == null)
            {
                return;
            }

            bridgeHost.TryDismissAttentionToast(_presentation.Active.CardId);
            Refresh();
        }

        /// <summary>Apply current session toast state onto labels (safe when unwired).</summary>
        public void Refresh()
        {
            if (bridgeHost != null)
            {
                _presentation = bridgeHost.RefreshAttentionToast();
            }

            ApplyPresentation();
        }

        private void ApplyPresentation()
        {
            var visible = showPanel && _presentation.HasActiveCard;
            if (_root != null)
            {
                _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_card != null)
            {
                _card.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                _card.RemoveFromClassList("c2-attention-toast-card--critical");
                _card.RemoveFromClassList("c2-attention-toast-card--notable");
                if (_presentation.Active is { Severity: AlertSeverity.Critical })
                {
                    _card.AddToClassList("c2-attention-toast-card--critical");
                }
                else if (_presentation.Active is { Severity: AlertSeverity.Notable })
                {
                    _card.AddToClassList("c2-attention-toast-card--notable");
                }
            }

            var active = _presentation.Active;
            if (_severity != null)
            {
                _severity.text = active?.SeverityLabel ?? string.Empty;
            }

            if (_title != null)
            {
                _title.text = active?.Title ?? string.Empty;
            }

            if (_body != null)
            {
                _body.text = active?.Body ?? string.Empty;
            }

            if (_queue != null)
            {
                _queue.text = _presentation.QueueBadge;
            }

            if (_ack != null)
            {
                _ack.SetEnabled(active != null);
            }

            if (_dismiss != null)
            {
                _dismiss.SetEnabled(active != null);
            }
        }
    }
}
#endif
