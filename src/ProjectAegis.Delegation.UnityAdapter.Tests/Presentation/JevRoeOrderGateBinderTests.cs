using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class JevRoeOrderGateBinderTests
{
    [Test]
    public void Pin_vs3_and_fixtures_pin_jev_1_13_x_and_declare_vs3_questions()
    {
        var pin = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "jev", "pins", "jev-1.13.x-vs3.json"));

        Assert.That(pin, Does.Contain("\"pinModel\": \"jev-1.13.x\""));
        Assert.That(pin, Does.Contain("\"openRouterModel\": \"typesafe/jev-1.13\""));
        Assert.That(pin, Does.Contain("\"package\": \"jev-1.13.x\""));
        Assert.That(pin, Does.Contain("\"roeNoulFloor\": 0.8"));
        Assert.That(pin, Does.Contain("suggest_ok_under_roe"));
        Assert.That(pin, Does.Contain("order_proposal_risk"));
        Assert.That(JevRoeOrderGateBinder.DefaultRoeNoulFloor, Is.EqualTo(0.80));
    }

    [Test]
    public void Valid_vs3_roe_noul_clears_card_without_auto_firing()
    {
        var json = ReadVs3Fixture("vs3-roe-clear.json");
        var state = JevRoeOrderGateBinder.Evaluate(json);

        Assert.That(state.CardVisible, Is.True);
        Assert.That(state.ConfirmEnabled, Is.True);
        Assert.That(state.IsAutoFire, Is.False);
        Assert.That(state.GroupId, Is.EqualTo("tg-baltic-escort"));
        Assert.That(state.Decision, Is.EqualTo(CoordinationDecision.Withdraw));
        Assert.That(state.RoeNoul, Is.EqualTo(0.88).Within(0.001));
        Assert.That(state.RiskLevel, Is.EqualTo("low"));
        Assert.That(state.ModelPin, Is.EqualTo(JevDisclosurePresentation.PinnedModel));
        Assert.That(state.OpenRouterModel, Is.EqualTo(JevDisclosurePresentation.PinnedOpenRouterModel));
        Assert.That(state.SummaryLine, Does.Contain("tg-baltic-escort"));
        Assert.That(state.BlockReason, Is.Null);
    }

    [Test]
    public void Low_noul_or_denied_roe_or_high_risk_or_reattack_fails_closed()
    {
        var lowNoul = JevRoeOrderGateBinder.Evaluate(ReadVs3Fixture("vs3-roe-low-noul.json"));
        Assert.That(lowNoul.CardVisible, Is.False);
        Assert.That(lowNoul.ConfirmEnabled, Is.False);
        Assert.That(lowNoul.IsAutoFire, Is.False);
        Assert.That(lowNoul.BlockReason, Is.EqualTo(JevRoeOrderGateBinder.ReasonRoeNoulBelowFloor));

        var deniedRoe = JevRoeOrderGateBinder.Evaluate(ReadVs3Fixture("vs3-roe-denied.json"));
        Assert.That(deniedRoe.CardVisible, Is.False);
        Assert.That(deniedRoe.ConfirmEnabled, Is.False);
        Assert.That(deniedRoe.IsAutoFire, Is.False);
        Assert.That(deniedRoe.BlockReason, Is.EqualTo(JevRoeOrderGateBinder.ReasonRoeSuggestDenied));

        var blockedRisk = JevRoeOrderGateBinder.Evaluate(ReadVs3Fixture("vs3-risk-blocked.json"));
        Assert.That(blockedRisk.CardVisible, Is.False);
        Assert.That(blockedRisk.ConfirmEnabled, Is.False);
        Assert.That(blockedRisk.IsAutoFire, Is.False);
        Assert.That(blockedRisk.BlockReason, Is.EqualTo(JevRoeOrderGateBinder.ReasonRiskBlocked));

        var reattackJson = """
            {
              "schema": "aegis.jev.vs3.state.v0",
              "skillAvailability": { "jevDecide": true },
              "proposal": { "groupId": "tg-baltic-escort", "decision": "Reattack" },
              "jevDecision": {
                "model": "typesafe/jev-1.13",
                "suggest_ok_under_roe": "yes",
                "roeNoul": 0.92,
                "order_proposal_risk": "low"
              }
            }
            """;
        var reattackState = JevRoeOrderGateBinder.Evaluate(reattackJson);
        Assert.That(reattackState.CardVisible, Is.False);
        Assert.That(reattackState.ConfirmEnabled, Is.False);
        Assert.That(reattackState.IsAutoFire, Is.False);
        Assert.That(reattackState.BlockReason, Is.EqualTo(JevRoeOrderGateBinder.ReasonAdvisoryDecisionOnly));
    }

    [Test]
    public void Offline_or_unpinned_model_or_private_payload_fails_closed()
    {
        var offline = JevRoeOrderGateBinder.Evaluate(ReadVs3Fixture("vs3-offline.json"));
        Assert.That(offline.CardVisible, Is.False);
        Assert.That(offline.ConfirmEnabled, Is.False);
        Assert.That(offline.IsAutoFire, Is.False);
        Assert.That(offline.BlockReason, Is.EqualTo(JevRoeOrderGateBinder.ReasonJevOffline));

        var unpinned = JevRoeOrderGateBinder.Evaluate(ReadVs3Fixture("vs3-unpinned-model.json"));
        Assert.That(unpinned.CardVisible, Is.False);
        Assert.That(unpinned.ConfirmEnabled, Is.False);
        Assert.That(unpinned.IsAutoFire, Is.False);
        Assert.That(unpinned.BlockReason, Is.EqualTo(JevRoeOrderGateBinder.ReasonUnpinnedModel));

        const string privatePayload = """
            {"schema":"aegis.jev.vs3.state.v0","decisionLog":[],"skillAvailability":{"jevDecide":true}}
            """;
        Assert.Throws<InvalidOperationException>(() => JevRoeOrderGateBinder.Evaluate(privatePayload));
    }

    [TestCase("999")]
    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("2")]
    [TestCase("Hold, Withdraw")]
    public void Numeric_and_undefined_decision_strings_fail_closed(string decisionValue)
    {
        var json = $$"""
            {
              "schema": "aegis.jev.vs3.state.v0",
              "skillAvailability": { "jevDecide": true },
              "proposal": { "groupId": "tg-baltic-escort", "decision": "{{decisionValue}}" },
              "jevDecision": {
                "model": "typesafe/jev-1.13",
                "suggest_ok_under_roe": "yes",
                "roeNoul": 0.92,
                "order_proposal_risk": "low"
              }
            }
            """;

        var state = JevRoeOrderGateBinder.Evaluate(json);

        Assert.That(state.CardVisible, Is.False);
        Assert.That(state.ConfirmEnabled, Is.False);
        Assert.That(state.IsAutoFire, Is.False);
        Assert.That(state.BlockReason, Is.EqualTo(JevRoeOrderGateBinder.ReasonInvalidProposal));
    }

    private static string ReadVs3Fixture(string fileName) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "tools", "jev", "fixtures", "vs3", fileName));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "repo root not found");
        return dir!.FullName;
    }
}
