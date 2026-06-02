// Doc-20 left drawer — tabbed OOB / missions / contacts (single UIDocument).
#if UNITY_5_3_OR_NEWER
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

        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;
        private ToolbarToggle? _tabOob;
        private ToolbarToggle? _tabMissions;
        private ToolbarToggle? _tabContacts;
        private ListView? _oobList;
        private ListView? _missionList;
        private ListView? _contactList;
        private OobTreePanelState _oobState = new(Array.Empty<OobTreeDisplayRow>());
        private MissionListPanelState _missionState = new(Array.Empty<MissionListDisplayRow>());
        private SensorC2PanelState _contactState = new("EMCON: —", "TRACK: —", "CONTACTS: 0", Array.Empty<SensorC2ContactRow>());
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
            _tabOob = panel.Q<ToolbarToggle>(TabOobName);
            _tabMissions = panel.Q<ToolbarToggle>(TabMissionsName);
            _tabContacts = panel.Q<ToolbarToggle>(TabContactsName);
            _oobList = panel.Q<ListView>(OobListName);
            _missionList = panel.Q<ListView>(MissionListName);
            _contactList = panel.Q<ListView>(ContactListName);

            WireList(_oobList);
            WireList(_missionList);
            WireContactList(_contactList);

            if (_tabOob != null)
            {
                _tabOob.RegisterValueChangedCallback(_ => SelectTab(DrawerTab.Oob));
            }

            if (_tabMissions != null)
            {
                _tabMissions.RegisterValueChangedCallback(_ => SelectTab(DrawerTab.Missions));
            }

            if (_tabContacts != null)
            {
                _tabContacts.RegisterValueChangedCallback(_ => SelectTab(DrawerTab.Contacts));
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
                    label.text = _oobState.UnitRows[index].DisplayLine;
                }
                else if (listView == _missionList && index >= 0 && index < _missionState.MissionRows.Count)
                {
                    label.text = _missionState.MissionRows[index].DisplayLine;
                }
            };
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
                    label.text = _contactState.ContactRows[index].DisplayLine;
                }
            };
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

            _oobState = OobTreePanelBinder.Bind(bridgeHost.LastOobTree);
            _missionState = MissionListPanelBinder.Bind(bridgeHost.LastMissionList);
            _contactState = SensorC2PanelBinder.Bind(bridgeHost.LastSensorC2);

            _oobList!.itemsSource = _oobState.UnitRows;
            _missionList!.itemsSource = _missionState.MissionRows;
            _contactList!.itemsSource = _contactState.ContactRows;
            _oobList.Rebuild();
            _missionList.Rebuild();
            _contactList.Rebuild();

            var root = _document.rootVisualElement?.Q(RootName);
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
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