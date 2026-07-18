// Doc-20 left drawer — tabbed OOB / missions / contacts (single UIDocument).
// S38-04 C2/Platform polish residual (density/filters from S37 carry): part of C2 track. Per sprint-38 + qa-plan + polish-scope-boundary-2026-06-19.md (lean, isolated).
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
        private const string OobListName = "oob-list";
        private const string MissionListName = "mission-list";
        private const string ContactListName = "contact-list";
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
        private Toggle? _tabOob;
        private Toggle? _tabMissions;
        private Toggle? _tabContacts;
        private ListView? _oobList;
        private ListView? _missionList;
        private ListView? _contactList;
        private OobTreePanelState _oobState = new(Array.Empty<OobTreeDisplayRow>());
        private MissionListPanelState _missionState = new(Array.Empty<MissionListDisplayRow>());
        private SensorC2PanelState _contactState = new("EMCON: —", "TRACK: —", "CONTACTS: 0", Array.Empty<SensorC2ContactRow>());
        private C2PlanningChromeState _planningChrome = new(false, false, SimulationPhase.Planning);
        private bool _wired;

        /// <summary>True while drawer tabs are view-only during <see cref="SimulationPhase.Planning"/> (S30-07).</summary>
        public bool IsDrawerReadOnly => _planningChrome.IsDrawerReadOnly;

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
            SelectTab(DrawerTab.Oob);
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
            _tabOob = panel.Q<Toggle>(TabOobName);
            _tabMissions = panel.Q<Toggle>(TabMissionsName);
            _tabContacts = panel.Q<Toggle>(TabContactsName);
            _oobList = panel.Q<ListView>(OobListName);
            _missionList = panel.Q<ListView>(MissionListName);
            _contactList = panel.Q<ListView>(ContactListName);

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
        }

        private void WireList(ListView? listView)
        {
            if (listView == null)
            {
                return;
            }

            listView.makeItem = () => new Label();
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

            SetListVisible(_oobList, tab == DrawerTab.Oob);
            SetListVisible(_missionList, tab == DrawerTab.Missions);
            SetListVisible(_contactList, tab == DrawerTab.Contacts);
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
                return;
            }

            _oobState = OobTreePanelBinder.Bind(bridgeHost.LastOobTree, bridgeHost.SelectedUnitId);
            _missionState = MissionListPanelBinder.Bind(bridgeHost.LastMissionList);
            _contactState = SensorC2PanelBinder.Bind(bridgeHost.LastSensorC2);

            _oobList!.itemsSource = _oobState.UnitRows.ToList();
            _missionList!.itemsSource = _missionState.MissionRows.ToList();
            _contactList!.itemsSource = _contactState.ContactRows.ToList();
            _oobList.Rebuild();
            _missionList.Rebuild();
            _contactList.Rebuild();

            ApplyPlanningChrome();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
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