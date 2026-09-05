// Doc-20 top bar — sim time, phase, compression, score strip; CMD-22 Zulu/local/remain.
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class C2TopBarPanelHost : MonoBehaviour
    {
        private const string RootName = "c2-topbar-root";
        private const string SimTimeName = "sim-time-label";
        private const string PhaseName = "phase-label";
        private const string BeginExecutionName = "begin-execution-button";
        private const string CompressionName = "compression-label";
        private const string CompressionSlowerName = "compression-slower-button";
        private const string CompressionFasterName = "compression-faster-button";
        private const string PauseResumeName = "pause-resume-button";
        private const string ModeName = "mode-label";
        private const string CommsName = "comms-label";
        private const string ScoreName = "score-label";
        private const string ZuluTimeName = "zulu-time-label";
        private const string LocalTimeName = "local-time-label";
        private const string RemainingDurationName = "remaining-duration-label";

        /// <summary>Repo-relative Approved production USS for ASSET-005 (Path A graduate).</summary>
        public const string ApprovedProductionUssRelativePath =
            ProjectAegis.Delegation.Projection.ApprovedC2AssetPaths.Asset005TopBarUss;

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private Label? _simTime;
        private Label? _phase;
        private Button? _beginExecution;
        private Label? _compression;
        private Button? _compressionSlower;
        private Button? _compressionFaster;
        private Button? _pauseResume;
        private Label? _mode;
        private Label? _comms;
        private Label? _score;
        private Label? _zuluTime;
        private Label? _localTime;
        private Label? _remainingDuration;
        private bool _wired;
        private C2TopBarPresentation _presentation = C2TopBarPresentation.Empty;

        /// <summary>True after panel stylesheet has been applied to the top-bar root.</summary>
        public bool StylesApplied { get; private set; }

        /// <summary>Last applied presentation fields (headless-readable after ApplyPanelState).</summary>
        public C2TopBarPresentation LastPresentation => _presentation;

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
                StylesApplied = true;
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
            _simTime = panel.Q<Label>(SimTimeName);
            _phase = panel.Q<Label>(PhaseName);
            _beginExecution = panel.Q<Button>(BeginExecutionName);
            _compression = panel.Q<Label>(CompressionName);
            _compressionSlower = panel.Q<Button>(CompressionSlowerName);
            _compressionFaster = panel.Q<Button>(CompressionFasterName);
            _pauseResume = panel.Q<Button>(PauseResumeName);
            _mode = panel.Q<Label>(ModeName);
            _comms = panel.Q<Label>(CommsName);
            _score = panel.Q<Label>(ScoreName);
            // CMD-22: null-safe Q — labels optional for older UXML trees.
            _zuluTime = panel.Q<Label>(ZuluTimeName);
            _localTime = panel.Q<Label>(LocalTimeName);
            _remainingDuration = panel.Q<Label>(RemainingDurationName);
            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
                StylesApplied = true;
            }

            if (_beginExecution != null)
            {
                _beginExecution.clicked -= OnBeginExecutionClicked;
                _beginExecution.clicked += OnBeginExecutionClicked;
            }

            if (_compressionSlower != null)
            {
                _compressionSlower.clicked -= OnCompressionSlowerClicked;
                _compressionSlower.clicked += OnCompressionSlowerClicked;
            }

            if (_compressionFaster != null)
            {
                _compressionFaster.clicked -= OnCompressionFasterClicked;
                _compressionFaster.clicked += OnCompressionFasterClicked;
            }

            if (_pauseResume != null)
            {
                _pauseResume.clicked -= OnPauseResumeClicked;
                _pauseResume.clicked += OnPauseResumeClicked;
            }

            _wired = _simTime != null && _phase != null && _compression != null && _mode != null && _score != null;
        }

        /// <summary>
        /// Ensures Approved-style stylesheet is on the panel root (ASSET-005 parity).
        /// </summary>
        public bool EnsurePanelStylesApplied()
        {
            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null || panelStyles == null)
            {
                return StylesApplied;
            }

            var panel = root.Q<VisualElement>(RootName) ?? root;
            if (!panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            StylesApplied = true;
            return true;
        }

        private void OnBeginExecutionClicked()
        {
            if (bridgeHost == null || bridgeHost.Phase != SimulationPhase.Planning)
            {
                return;
            }

            bridgeHost.BeginExecution();
            Refresh();
        }

        private void OnCompressionSlowerClicked()
        {
            if (bridgeHost == null)
            {
                return;
            }

            var current = bridgeHost.Session?.TimeAccelerationFactor ?? 1;
            bridgeHost.TrySetTimeAcceleration(C2ClockCommand.NextSlower(current), out _);
            Refresh();
        }

        private void OnCompressionFasterClicked()
        {
            if (bridgeHost == null)
            {
                return;
            }

            var current = bridgeHost.Session?.TimeAccelerationFactor ?? 1;
            bridgeHost.TrySetTimeAcceleration(C2ClockCommand.NextFaster(current), out _);
            Refresh();
        }

        private void OnPauseResumeClicked()
        {
            if (bridgeHost == null)
            {
                return;
            }

            if (bridgeHost.Session?.IsSimPaused == true)
            {
                bridgeHost.TryResumeSim(explicitOverride: false, out _);
            }
            else
            {
                bridgeHost.TryPauseSim(out _);
            }

            Refresh();
        }

        /// <summary>
        /// Apply projected top-bar state via headless <see cref="C2TopBarApplyState"/> (S106).
        /// Safe when UIDocument labels are not yet wired (presentation still recorded).
        /// </summary>
        public void ApplyPanelState(C2TopBarState? state)
        {
            _presentation = C2TopBarApplyState.Apply(state);
            ApplyPresentationToLabels();
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null || bridgeHost.Bridge == null)
            {
                return;
            }

            // Prefer bridge-projected state through the shipped apply path so label fields
            // match C2TopBarProjection output; live phase always wins on the host.
            var state = bridgeHost.LastTopBar;
            var applied = C2TopBarApplyState.Apply(state) with
            {
                PhaseLabel = $"PHASE: {bridgeHost.Phase}",
            };
            var comms = bridgeHost.LastCommsState;
            if (comms != null)
            {
                applied = applied with
                {
                    CommsLabel = comms.TopBarLabel,
                    CommsCssClass = C2TopBarApplyState.ResolveCommsCssClass(comms.TopBarLabel),
                };
            }

            _presentation = applied;

            ApplyPresentationToLabels();
            OverlayLiveCompression();
            RefreshBeginExecutionButton();
            RefreshPauseResumeButton();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplyPresentationToLabels()
        {
            if (_simTime != null)
            {
                _simTime.text = _presentation.SimTimeLabel;
            }

            if (_phase != null)
            {
                _phase.text = _presentation.PhaseLabel;
            }

            if (_compression != null)
            {
                _compression.text = _presentation.CompressionLabel;
            }

            if (_mode != null)
            {
                _mode.text = _presentation.ModeLabel;
            }

            if (_score != null)
            {
                _score.text = _presentation.ScoreLabel;
            }

            if (_zuluTime != null)
            {
                _zuluTime.text = _presentation.ZuluTimeLabel;
            }

            if (_localTime != null)
            {
                _localTime.text = _presentation.LocalTimeLabel;
            }

            if (_remainingDuration != null)
            {
                _remainingDuration.text = _presentation.RemainingDurationLabel;
            }

            if (_comms != null)
            {
                _comms.text = _presentation.CommsLabel;
                _comms.ClearClassList();
                _comms.AddToClassList("c2-topbar-item");
                _comms.AddToClassList("c2-topbar-item--comms");
                _comms.AddToClassList(_presentation.CommsCssClass);
            }
        }

        private void RefreshBeginExecutionButton()
        {
            if (_beginExecution == null)
            {
                return;
            }

            var isPlanning = bridgeHost!.Phase == SimulationPhase.Planning;
            _beginExecution.style.display = isPlanning ? DisplayStyle.Flex : DisplayStyle.None;
            _beginExecution.SetEnabled(isPlanning);
        }

        private void OverlayLiveCompression()
        {
            if (_compression == null || bridgeHost == null)
            {
                return;
            }

            _compression.text = bridgeHost.LiveCompressionLabel;
        }

        private void RefreshPauseResumeButton()
        {
            if (_pauseResume == null || bridgeHost == null)
            {
                return;
            }

            var paused = bridgeHost.Session != null && bridgeHost.Session.IsSimPaused;
            var canResume = !bridgeHost.LastAttentionToast.HasUnresolvedPauseClass
                || bridgeHost.LastAttentionToast.CanResume;
            _pauseResume.text = paused ? "RESUME" : "PAUSE";
            _pauseResume.SetEnabled(!paused || canResume);
            _pauseResume.tooltip = paused && !canResume
                ? "Acknowledge the attention toast to resume"
                : string.Empty;
        }
    }
}
#endif
