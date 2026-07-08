// req 20 AC-1 / F5 — bind point for the shared PanelSettings asset (Scale With Screen Size,
// reference 1920x1080, match=height; see Assets/UI/AegisSharedPanelSettings.asset). Attach
// alongside a UIDocument to apply the shared settings without hand-wiring every host's Inspector
// field. Purely additive — does not touch any host's selection/filter logic.
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AegisPanelSettingsBinder : MonoBehaviour
    {
        [SerializeField] private PanelSettings? sharedPanelSettings;

        [Tooltip("When true, overwrite a PanelSettings already assigned on the UIDocument in the Inspector. Default false — an explicit per-scene assignment wins.")]
        [SerializeField] private bool overrideExisting;

        private void Awake()
        {
            if (sharedPanelSettings == null)
            {
                return;
            }

            var document = GetComponent<UIDocument>();
            if (document == null)
            {
                return;
            }

            if (document.panelSettings == null || overrideExisting)
            {
                document.panelSettings = sharedPanelSettings;
            }
        }
    }
}
#endif
