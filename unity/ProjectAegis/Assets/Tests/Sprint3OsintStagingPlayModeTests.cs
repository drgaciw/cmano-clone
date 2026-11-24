#if UNITY_5_3_OR_NEWER
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint3OsintStagingPlayModeTests
    {
        private GameObject? _go;
        private PanelSettings? _settings;
        private ThemeStyleSheet? _theme;
        private string? _tempDbPath;
        private string? _tempFixturePath;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            if (_settings != null) UnityEngine.Object.DestroyImmediate(_settings);
            if (_theme != null) UnityEngine.Object.DestroyImmediate(_theme);
            _go = null;
            _settings = null;
            _theme = null;

            if (!string.IsNullOrEmpty(_tempDbPath) && File.Exists(_tempDbPath))
            {
                try { File.Delete(_tempDbPath); } catch { }
            }
            if (!string.IsNullOrEmpty(_tempFixturePath) && File.Exists(_tempFixturePath))
            {
                try { File.Delete(_tempFixturePath); } catch { }
            }
        }

        private UIDocument CreateDocument(GameObject host)
        {
            var document = host.AddComponent<UIDocument>();
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            _settings.themeStyleSheet = _theme;
            document.panelSettings = _settings;
            return document;
        }

        [UnityTest]
        public IEnumerator OsintStagingPanel_enforces_sorting_order_17_and_displays_confidence_and_citation_for_selected_proposal()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"aegis-pm-osint-{Guid.NewGuid():N}.db");
            var staged = OsintStagingBridge.ProposeFixtureRecord(
                _tempDbPath,
                canonicalId: "hypersonic-glide-s20",
                sourceUrl: "https://ex.com/hg-s20",
                snippet: "observed boost-glide",
                relevanceScore: 0.81,
                targetDoc: "10",
                proposedTrl: 7,
                actorType: "osint",
                actorId: "pm-test",
                markApprovedForHumanReview: true,
                clockTicks: 5000);
            Assert.IsTrue(staged.Succeeded);

            _go = new GameObject("sprint3-osint-staging-test");
            var document = CreateDocument(_go);
            BuildOsintTree(
                document.rootVisualElement,
                out var confidenceLabel,
                out var citationUrlLabel,
                out var snippetLabel,
                out var approveButton,
                out var rejectButton,
                out var statusLabel);

            var panelHost = _go.AddComponent<OsintStagingPanelHost>();
            panelHost.enabled = false;
            panelHost.BindDatabasePath(_tempDbPath);
            panelHost.enabled = true;
            yield return null;

            Assert.AreEqual(17, document.sortingOrder);
            Assert.IsTrue(panelHost.IsWired);
            Assert.GreaterOrEqual(panelHost.ProposalCount, 1);
            StringAssert.Contains("0.81", confidenceLabel.text);
            StringAssert.Contains("https://ex.com/hg-s20", citationUrlLabel.text);
            Assert.IsFalse(string.IsNullOrWhiteSpace(snippetLabel.text));
            Assert.IsTrue(approveButton.enabledSelf);
            Assert.IsTrue(rejectButton.enabledSelf);
            Assert.IsNotNull(statusLabel);
        }

        [UnityTest]
        public IEnumerator OsintStagingPanel_keeps_sub_threshold_items_log_only_with_approve_disabled()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"aegis-pm-osint-logonly-{Guid.NewGuid():N}.db");
            _tempFixturePath = Path.Combine(Path.GetTempPath(), $"aegis-pm-osint-fix-{Guid.NewGuid():N}.json");
            File.WriteAllText(_tempFixturePath,
                "[{\"canonicalId\":\"high-conf\",\"sourceUrl\":\"https://ex.com/high\",\"snippet\":\"verified sensor\",\"relevanceScore\":0.81,\"targetDoc\":\"10\",\"proposedTrl\":7}," +
                "{\"canonicalId\":\"low-conf\",\"sourceUrl\":\"https://ex.com/low\",\"snippet\":\"below threshold\",\"relevanceScore\":0.40,\"targetDoc\":\"10\",\"proposedTrl\":4}]");

            _go = new GameObject("sprint3-osint-logonly-test");
            var document = CreateDocument(_go);
            BuildOsintTree(
                document.rootVisualElement,
                out var confidenceLabel,
                out var citationUrlLabel,
                out _,
                out var approveButton,
                out _,
                out _);

            var panelHost = _go.AddComponent<OsintStagingPanelHost>();
            panelHost.enabled = false;
            panelHost.BindPaths(_tempDbPath, _tempFixturePath);
            panelHost.enabled = true;
            yield return null;

            Assert.AreEqual(1, panelHost.ProposalCount);
            Assert.AreEqual(1, panelHost.LogOnlyCount);

            var logOnlyList = document.rootVisualElement.Q<ListView>("logonly-list");
            Assert.IsNotNull(logOnlyList);
            logOnlyList.selectedIndex = 0;
            yield return null;

            StringAssert.Contains("0.40", confidenceLabel.text);
            StringAssert.Contains("https://ex.com/low", citationUrlLabel.text);
            Assert.IsFalse(approveButton.enabledSelf);
        }

        [UnityTest]
        public IEnumerator OsintStagingPanel_approve_and_reject_route_through_write_gate_and_refresh()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"aegis-pm-osint-gate-{Guid.NewGuid():N}.db");
            var staged1 = OsintStagingBridge.ProposeFixtureRecord(
                _tempDbPath, "sensor-approve", "https://ex.com/s1", "approve snippet", 0.85, "10", 7,
                "osint", "pm-test", markApprovedForHumanReview: true, clockTicks: 6001);
            var staged2 = OsintStagingBridge.ProposeFixtureRecord(
                _tempDbPath, "sensor-reject", "https://ex.com/s2", "reject snippet", 0.78, "10", 6,
                "osint", "pm-test", markApprovedForHumanReview: true, clockTicks: 6002);
            Assert.IsTrue(staged1.Succeeded && staged2.Succeeded);

            _go = new GameObject("sprint3-osint-gate-test");
            var document = CreateDocument(_go);
            BuildOsintTree(
                document.rootVisualElement,
                out _, out _, out _,
                out var approveButton,
                out var rejectButton,
                out var statusLabel);

            var panelHost = _go.AddComponent<OsintStagingPanelHost>();
            panelHost.enabled = false;
            panelHost.BindPaths(_tempDbPath, string.Empty);
            panelHost.enabled = true;
            yield return null;

            Assert.AreEqual(2, panelHost.ProposalCount);

            SendSubmit(approveButton);
            yield return null;
            Assert.AreEqual(1, panelHost.ProposalCount);
            StringAssert.Contains("COMMITTED", statusLabel.text);

            SendSubmit(rejectButton);
            yield return null;
            Assert.AreEqual(0, panelHost.ProposalCount);
            StringAssert.Contains("REJECTED", statusLabel.text);
        }

        private static void BuildOsintTree(
            VisualElement root,
            out Label confidenceLabel,
            out Label citationUrlLabel,
            out Label snippetLabel,
            out Button approveButton,
            out Button rejectButton,
            out Label statusLabel)
        {
            var container = new VisualElement { name = "osint-staging-root" };
            statusLabel = new Label { name = "status-line" };
            var proposalList = new ListView { name = "proposal-list" };
            var logOnlyList = new ListView { name = "logonly-list" };
            var detailCard = new VisualElement { name = "osint-detail-card" };
            var detailTitle = new Label { name = "osint-detail-title" };
            confidenceLabel = new Label { name = "osint-detail-confidence" };
            var trlLabel = new Label { name = "osint-detail-trl" };
            citationUrlLabel = new Label { name = "osint-detail-citation-url" };
            snippetLabel = new Label { name = "osint-detail-citation-snippet" };
            approveButton = new Button { name = "approve-selected", text = "Approve" };
            rejectButton = new Button { name = "reject-selected", text = "Reject" };
            var refreshButton = new Button { name = "refresh-button", text = "Refresh" };

            detailCard.Add(detailTitle);
            detailCard.Add(confidenceLabel);
            detailCard.Add(trlLabel);
            detailCard.Add(citationUrlLabel);
            detailCard.Add(snippetLabel);

            container.Add(statusLabel);
            container.Add(proposalList);
            container.Add(detailCard);
            container.Add(approveButton);
            container.Add(rejectButton);
            container.Add(refreshButton);
            container.Add(logOnlyList);
            root.Add(container);
        }

        private static void SendSubmit(Button button)
        {
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }
    }
}
#endif
