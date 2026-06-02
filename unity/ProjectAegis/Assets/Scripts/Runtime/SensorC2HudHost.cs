// Minimal sensor C2 HUD — OnGUI list until UI Toolkit assets land (Sprint 2 slice).
#if UNITY_5_3_OR_NEWER
using System.Text;
using UnityEngine;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SensorC2HudHost : MonoBehaviour
    {
        [SerializeField] private DelegationBridgeHost bridgeHost = null!;
        [SerializeField] private bool showHud = true;

        private void Reset()
        {
            if (bridgeHost == null)
            {
                bridgeHost = GetComponent<DelegationBridgeHost>();
            }
        }

        private void OnGUI()
        {
            if (!showHud || bridgeHost == null)
            {
                return;
            }

            var c2 = bridgeHost.LastSensorC2;
            var builder = new StringBuilder(256);
            builder.AppendLine("SENSOR C2");
            builder.Append("EMCON: ").Append(c2.ObserverRadarEmconActive ? "ACTIVE" : "OFF").AppendLine();
            builder.Append("Track: ").Append(c2.HasFireControlTrackOnPrimaryContact ? "FC" : "—").AppendLine();
            builder.Append("Contacts: ").Append(c2.ActiveContactCount).AppendLine();
            foreach (var contact in c2.Contacts)
            {
                builder.Append("  ").Append(contact.ContactId)
                    .Append(' ').Append(contact.LifecycleState)
                    .Append(" → ").Append(contact.TargetId)
                    .AppendLine();
            }

            GUI.Box(new Rect(8, 8, 320, 24 + 18 * (4 + c2.Contacts.Count)), builder.ToString());
        }
    }
}
#endif