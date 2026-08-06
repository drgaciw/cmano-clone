// Doc-20 left drawer — tabbed OOB / missions / contacts (single UIDocument).
// S38-04 C2/Platform polish residual (density/filters from S37 carry): part of C2 track. Per sprint-38 + qa-plan + polish-scope-boundary-2026-06-19.md (lean, isolated).
// CMD-23: collapsible body via C2ChromeCollapseState + prefs bag on bridge host.
#if UNITY_5_3_OR_NEWER
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class C2LeftDrawerPanelHost : MonoBehaviour
    {
        private const string RootName = "c2-drawer-root";
        private const string TabOobName = "tab-oob";
        private const string TabMissionsName = "tab-missions";
        private const string TabContactsName = "tab-contacts";
        private const string TabsRowName = "c2-drawer-tabs";
        private const string OobListName = "oob-list";
        private const string MissionListName = "mission-list";
        private const string ContactListName = "contact-list";
        private const string CollapseToggleName = "c2-drawer-collapse-toggle";
        private const string CollapseToggleClass = "c2-drawer-collapse-toggle";
        private const string CollapsedClass = "c2-drawer-panel--collapsed";
        private const string HiddenClass = "c2-drawer-list--hidden";
        private const string PlanningReadOnlyClass = "c2-drawer-panel--planning-readonly";

        /// <summary>Repo-relative Specced production USS for ASSET-007 (not Approved).</summary>
        public const string SpeccedProductionUssRelativePath =
            ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths.Asset007LeftDrawerUss;

        /// <summary>Unity-side USS path constant for host style-path parity (S106).</summary>
        public const string UnityUssRelativePath =
            ProjectAegis.Delegation.Projection.SpeccedC2PanelStylePaths.UnityLeftDrawerUss;

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private VisualElement? _panelRoot;
        private VisualElement? _tabsRow;
        private Toggle? _tabOob;
        private Toggle? _tabMissions;
        private Toggle? _tabContacts;
        private ListView? _oobList;
        private ListView? _missionList;
        private ListView? _contactList;
        private Button? _collapseToggle;
        private OobTreePanelState _oobState = new(Array.Empty<OobTreeDisplayRow>());
        private MissionListPanelState _missionState = new(Array.Empty<MissionListDisplayRow>());
        private SensorC2PanelState _contactState = new("EMCON: —", "TRACK: —", "CONTACTS: 0", Array.Empty<SensorC2ContactRow>());
        private C2PlanningChromeState _planningChrome = new(false, false, SimulationPhase.Planning);
        private LeftDrawerPresentation _oobPresentation = LeftDrawerPresentation.Empty;
        private bool _wired;

        /// <summary>True while drawer tabs are view-only during <see cref="SimulationPhase.Planning"/> (S30-07).</summary>
        public bool IsDrawerReadOnly => _planningChrome.IsDrawerReadOnly;

        /// <summary>Last applied OOB presentation via <see cref="LeftDrawerApplyState"/> (S107).</summary>
        public LeftDrawerPresentation LastOobPresentation => _oobPresentation;

        /// <summary>Last applied chrome collapse presentation (CMD-23).</summary>
        public C2ChromeCollapsePresentation LastChromePresentation { get; private set; } =
            C2ChromeCollapsePresentation.Empty;

        /// <summary>Apply OOB panel state through the shipped apply-state path (S107).</summary>
        public void ApplyOobPanelState(OobTreePanelState? state)
        {
            // A non-null state with null UnitRows would NRE in Refresh() at
            // _oobState.UnitRows.ToList().
            _oobState = (state == null || state.UnitRows == null)
                ? new OobTreePanelState(Array.Empty<OobTreeDisplayRow>())
                : state;
            _oobPresentation = LeftDrawerApplyState.Apply(_oobState);

            // Direct-apply callers (tests / offline previews) never reach Refresh(),
            // so the visible rows would stay stale. Rebind here when the list is wired.
            if (_oobList != null)
            {
                _oobList.itemsSource = _oobState.UnitRows.ToList();
                _oobList.Rebuild();
            }
        }

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
            SelectTab(DrawerTab.Oob);
            Refresh();
        }

        private void LateUpdate()
        {
            if (!_wired)
            {
                TryWireElements();
            }

            if (!showPanel)
            {
                ApplyMasterVisibility();
                return;
            }

            if (bridgeHost == null)
            {
                // Null-safe chrome: expanded defaults under showPanel master visibility.
                ApplyMasterVisibility();
                ApplyChromeCollapse();
                return;
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
            _panelRoot = panel;
            _tabsRow = panel.Q(TabsRowName);
            _tabOob = panel.Q<Toggle>(TabOobName);
            _tabMissions = panel.Q<Toggle>(TabMissionsName);
            _tabContacts = panel.Q<Toggle>(TabContactsName);
            _oobList = panel.Q<ListView>(OobListName);
            _missionList = panel.Q<ListView>(MissionListName);
            _contactList = panel.Q<ListView>(ContactListName);
            EnsureCollapseToggle(panel);

            WireList(_oobList);
            WireList(_missionList);
            WireContactList(_contactList);

            if (_tabOob != null)
            {
                _tabOob.RegisterValueChangedCallback(evt => OnTabChanged(DrawerTab.Oob, evt.newValue));
            }

            if (_tabMissions != null)
            {
                _tabMissions.RegisterValueChangedCallback(evt => OnTabChanged(DrawerTab.Missions, evt.newValue));
            }

            if (_tabContacts != null)
            {
                _tabContacts.RegisterValueChangedCallback(evt => OnTabChanged(DrawerTab.Contacts, evt.newValue));
            }

            if (panelStyles != null && !panel.styleSheets.Contains(panelStyles))
            {
                panel.styleSheets.Add(panelStyles);
            }

            _wired = _tabOob != null && _oobList != null && _missionList != null && _contactList != null;
            ApplyMasterVisibility();
            ApplyChromeCollapse();
        }

        private void EnsureCollapseToggle(VisualElement panel)
        {
            _collapseToggle = panel.Q<Button>(CollapseToggleName);
            if (_collapseToggle == null)
            {
                _collapseToggle = new Button
                {
                    name = CollapseToggleName,
                    text = C2ChromeCollapseProjection.CollapseLeftDrawerLabel,
                };
                _collapseToggle.AddToClassList(CollapseToggleClass);
                _collapseToggle.focusable = true;
                // Title strip at top of drawer (body collapses under it).
                panel.Insert(0, _collapseToggle);
            }

            _collapseToggle.clicked -= OnCollapseToggleClicked;
            _collapseToggle.clicked += OnCollapseToggleClicked;
        }

        private void OnCollapseToggleClicked()
        {
            // Null-safe: no bridge chrome API → no-op (showPanel remains master visibility).
            if (bridgeHost == null)
            {
                return;
            }

            bridgeHost.ToggleLeftDrawerCollapsed();
            ApplyChromeCollapse();
        }

        private void WireList(ListView? listView)
        {
            if (listView == null)
            {
                return;
            }

            listView.makeItem = () =>
            {
                var label = new Label();
                label.focusable = listView == _oobList;
                if (listView == _oobList)
                {
                    label.RegisterCallback<KeyDownEvent>(OnOobRowKeyDown);
                }

                return label;
            };
            listView.bindItem = (element, index) =>
            {
                if (element is not Label label)
                {
                    return;
                }

                if (listView == _oobList && index >= 0 && index < _oobState.UnitRows.Count)
                {
                    var row = _oobState.UnitRows[index];
                    label.text = row.DisplayLine;
                    label.ClearClassList();
                    label.AddToClassList(row.StyleClass);
                    if (!row.IsAlive)
                    {
                        label.AddToClassList("oob-row--dead");
                    }

                    label.userData = row.UnitId;
                    label.UnregisterCallback<ClickEvent>(OnOobRowClicked);
                    label.RegisterCallback<ClickEvent>(OnOobRowClicked);
                }
                else if (listView == _missionList && index >= 0 && index < _missionState.MissionRows.Count)
                {
                    label.text = _missionState.MissionRows[index].DisplayLine;
                }
            };
        }

        private void OnOobRowClicked(ClickEvent evt)
        {
            if (evt.currentTarget is Label { userData: string unitId } && bridgeHost != null)
            {
                bridgeHost.SelectUnit(unitId);
            }
        }

        private void OnOobRowKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space))
            {
                return;
            }

            if (evt.currentTarget is Label { userData: string unitId } && bridgeHost != null)
            {
                bridgeHost.SelectUnit(unitId);
                evt.StopPropagation();
            }
        }

        private void WireContactList(ListView? listView)
        {
            if (listView == null)
            {
                return;
            }

            listView.makeItem = () => new Label();
            listView.bindItem = (element, index) =>
            {
                if (element is Label label && index >= 0 && index < _contactState.ContactRows.Count)
                {
                    var row = _contactState.ContactRows[index];
                    label.text = row.DisplayLine;
                    label.userData = row.ContactId;
                    label.UnregisterCallback<ClickEvent>(OnContactRowClicked);
                    label.RegisterCallback<ClickEvent>(OnContactRowClicked);
                }
            };
        }

        private void OnContactRowClicked(ClickEvent evt)
        {
            if (evt.currentTarget is Label { userData: string contactId } && bridgeHost != null)
            {
                bridgeHost.SelectContact(contactId);
            }
        }

        private void OnTabChanged(DrawerTab tab, bool selected)
        {
            if (!selected || _planningChrome.IsDrawerReadOnly)
            {
                return;
            }

            SelectTab(tab);
        }

        private void SelectTab(DrawerTab tab)
        {
            if (_tabOob != null)
            {
                _tabOob.SetValueWithoutNotify(tab == DrawerTab.Oob);
            }

            if (_tabMissions != null)
            {
                _tabMissions.SetValueWithoutNotify(tab == DrawerTab.Missions);
            }

            if (_tabContacts != null)
            {
                _tabContacts.SetValueWithoutNotify(tab == DrawerTab.Contacts);
            }

            // Tab visibility is subordinate to chrome collapse (body hidden when collapsed).
            var bodyVisible = !IsBodyCollapsed();
            SetListVisible(_oobList, bodyVisible && tab == DrawerTab.Oob);
            SetListVisible(_missionList, bodyVisible && tab == DrawerTab.Missions);
            SetListVisible(_contactList, bodyVisible && tab == DrawerTab.Contacts);
        }

        private static void SetListVisible(ListView? list, bool visible)
        {
            if (list == null)
            {
                return;
            }

            if (visible)
            {
                list.RemoveFromClassList(HiddenClass);
            }
            else
            {
                list.AddToClassList(HiddenClass);
            }
        }

        private void Refresh()
        {
            if (!_wired || bridgeHost == null)
            {
                ApplyMasterVisibility();
                ApplyChromeCollapse();
                return;
            }

            _oobState = OobTreePanelBinder.Bind(bridgeHost.LastOobTree, bridgeHost.SelectedUnitId);
            _oobPresentation = LeftDrawerApplyState.Apply(_oobState);
            _missionState = MissionListPanelBinder.Bind(bridgeHost.LastMissionList);
            _contactState = SensorC2PanelBinder.Bind(bridgeHost.LastSensorC2);

            // Skip list rebuild when collapsed (body hidden).
            if (!bridgeHost.ChromeCollapse.LeftDrawerCollapsed)
            {
                _oobList!.itemsSource = _oobState.UnitRows.ToList();
                _missionList!.itemsSource = _missionState.MissionRows.ToList();
                _contactList!.itemsSource = _contactState.ContactRows.ToList();
                _oobList.Rebuild();
                _missionList.Rebuild();
                _contactList.Rebuild();
            }

            ApplyPlanningChrome();
            ApplyMasterVisibility();
            ApplyChromeCollapse();
        }

        private void ApplyMasterVisibility()
        {
            if (_panelRoot != null)
            {
                _panelRoot.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// CMD-23: collapse drawer body (tabs + lists) to title/affordance strip.
        /// Null-safe when bridge has no chrome API — falls back to expanded body under showPanel.
        /// </summary>
        private void ApplyChromeCollapse()
        {
            var state = bridgeHost != null
                ? bridgeHost.ChromeCollapse
                : C2ChromeCollapseState.Expanded;
            LastChromePresentation = C2ChromeCollapseApplyState.Apply(state);

            var collapsed = showPanel && LastChromePresentation.LeftDrawerCollapsed;

            if (_tabsRow != null)
            {
                _tabsRow.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_collapseToggle != null)
            {
                _collapseToggle.text = LastChromePresentation.LeftDrawerAffordanceLabel;
                _collapseToggle.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_panelRoot != null)
            {
                if (collapsed)
                {
                    _panelRoot.AddToClassList(CollapsedClass);
                }
                else
                {
                    _panelRoot.RemoveFromClassList(CollapsedClass);
                }
            }

            // Re-apply tab body visibility under collapse (preserves selected tab when expanded).
            var active = DrawerTab.Oob;
            if (_tabMissions != null && _tabMissions.value)
            {
                active = DrawerTab.Missions;
            }
            else if (_tabContacts != null && _tabContacts.value)
            {
                active = DrawerTab.Contacts;
            }

            var bodyVisible = !collapsed;
            SetListVisible(_oobList, bodyVisible && active == DrawerTab.Oob);
            SetListVisible(_missionList, bodyVisible && active == DrawerTab.Missions);
            SetListVisible(_contactList, bodyVisible && active == DrawerTab.Contacts);
        }

        private bool IsBodyCollapsed()
        {
            if (!showPanel)
            {
                return true;
            }

            return bridgeHost != null && bridgeHost.ChromeCollapse.LeftDrawerCollapsed;
        }

        private void ApplyPlanningChrome()
        {
            if (bridgeHost == null)
            {
                return;
            }

            _planningChrome = C2PlanningChromeProjection.Project(bridgeHost.Phase);
            var root = _document.rootVisualElement?.Q(RootName);
            if (root == null)
            {
                return;
            }

            if (_planningChrome.IsDrawerReadOnly)
            {
                root.AddToClassList(PlanningReadOnlyClass);
            }
            else
            {
                root.RemoveFromClassList(PlanningReadOnlyClass);
            }

            var tabsReadOnly = _planningChrome.IsDrawerReadOnly;
            _tabOob?.SetEnabled(!tabsReadOnly);
            _tabMissions?.SetEnabled(!tabsReadOnly);
            _tabContacts?.SetEnabled(!tabsReadOnly);
        }

        private enum DrawerTab
        {
            Oob,
            Missions,
            Contacts,
        }
    }
}
#endif
