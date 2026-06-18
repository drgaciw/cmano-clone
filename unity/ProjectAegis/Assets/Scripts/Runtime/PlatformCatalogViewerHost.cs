// S26-10: ADR-011 Phase C read-only platform catalog browse (no write-gate bypass).
#if UNITY_5_3_OR_NEWER
using System.Linq;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlatformCatalogViewerHost : MonoBehaviour
    {
        private const string RootName = "platform-catalog-root";
        private const string ListName = "platform-catalog-list";

        [SerializeField] private bool showPanel = true;

        private UIDocument _document = null!;

        public void BindRows(IReadOnlyList<CatalogPlatformBrowseRow> rows)
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            var root = _document.rootVisualElement.Q(RootName);
            if (root == null)
            {
                root = new VisualElement { name = RootName };
                _document.rootVisualElement.Add(root);
            }

            root.Clear();
            root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;

            var items = rows.Select(r => $"{r.PlatformId} hp={r.MaxHp} speed={r.MaxSpeedKnots}").ToList();
            var list = new ListView
            {
                name = ListName,
                itemsSource = items,
                makeItem = () => new Label(),
                bindItem = (element, index) => ((Label)element).text = items[index],
            };
            root.Add(list);
        }

        public void BindReader(ICatalogReader reader) => BindRows(CatalogPlatformBrowseProjection.FromReader(reader));
    }
}
#endif