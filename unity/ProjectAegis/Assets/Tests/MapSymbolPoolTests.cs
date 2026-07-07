// EditMode tests for MapSymbolPool (fix #3, unity-integration-review): prove the map symbol
// renderer reuses VisualElements across syncs instead of clearing + recreating the canvas.
#if UNITY_5_3_OR_NEWER
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Unity.Runtime;

namespace ProjectAegis.Unity.Tests
{
    public sealed class MapSymbolPoolTests
    {
        private static MapSymbolDisplayRow Row(string id, float x = 0.5f) =>
            new(id, "■", id.ToUpperInvariant(), x, 0.5f, "friendly", false);

        private static List<MapSymbolDisplayRow> Rows(params MapSymbolDisplayRow[] rows) => rows.ToList();

        [Test]
        public void Sync_reuses_the_same_elements_on_identical_input()
        {
            var canvas = new VisualElement();
            var pool = new MapSymbolPool(canvas);

            pool.Sync(Rows(Row("a"), Row("b"), Row("c")), _ => { });
            Assert.AreEqual(3, canvas.childCount);
            var firstPass = canvas.Children().ToList();

            pool.Sync(Rows(Row("a"), Row("b"), Row("c")), _ => { });
            var secondPass = canvas.Children().ToList();

            Assert.AreEqual(3, canvas.childCount, "No net element change expected.");
            CollectionAssert.AreEquivalent(firstPass, secondPass,
                "Identical input should reuse the same VisualElement instances (no clear+recreate).");
        }

        [Test]
        public void Sync_adds_new_ids_and_removes_absent_ids()
        {
            var canvas = new VisualElement();
            var pool = new MapSymbolPool(canvas);

            pool.Sync(Rows(Row("a"), Row("b")), _ => { });
            Assert.AreEqual(2, pool.Count);

            pool.Sync(Rows(Row("a"), Row("c"), Row("d")), _ => { });
            Assert.AreEqual(3, pool.Count);
            Assert.AreEqual(3, canvas.childCount);

            var ids = canvas.Children().Select(e => (string)e.userData).ToList();
            CollectionAssert.AreEquivalent(new[] { "a", "c", "d" }, ids);
        }

        [Test]
        public void Sync_updates_position_of_a_reused_element_in_place()
        {
            var canvas = new VisualElement();
            var pool = new MapSymbolPool(canvas);

            pool.Sync(Rows(Row("a", 0.25f)), _ => { });
            var element = canvas.ElementAt(0);

            pool.Sync(Rows(Row("a", 0.75f)), _ => { });

            Assert.AreSame(element, canvas.ElementAt(0), "Same id must reuse the element, not recreate it.");
            Assert.AreEqual(75f, element.style.left.value.value, 0.01f, "Position should update in place.");
        }
    }
}
#endif
