using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Console;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Console;

[TestFixture]
public sealed class ConsoleNoiseClassifierTests
{
    [Test]
    public void Mcp_hub_spam_is_package_tool_and_ignorable()
    {
        var msg =
            "McpManagerClientHub Connection not available and auto-reconnect disabled for endpoint: /hub/mcp-server";
        Assert.That(ConsoleNoiseClassifier.Classify(msg), Is.EqualTo(ConsoleNoiseClassifier.NoiseClass.PackageTool));
        Assert.That(ConsoleNoiseClassifier.IsIgnorableForConsoleGate(msg), Is.True);
    }

    [Test]
    public void Account_API_timeout_is_environment_and_ignorable()
    {
        var msg =
            "Account API did not become accessible within 30 seconds. This may be due to network issues or editor focus.";
        Assert.That(ConsoleNoiseClassifier.Classify(msg), Is.EqualTo(ConsoleNoiseClassifier.NoiseClass.Environment));
        Assert.That(ConsoleNoiseClassifier.IsIgnorableForConsoleGate(msg), Is.True);
    }

    [Test]
    public void Licensing_client_404_is_environment_and_ignorable()
    {
        var msg =
            "[Licensing::Client] Error: Code 404 while processing request (status: Found 0 entitlement groups and 0 free entitlements matching requested entitlement ids)";
        Assert.That(ConsoleNoiseClassifier.Classify(msg), Is.EqualTo(ConsoleNoiseClassifier.NoiseClass.Environment));
        Assert.That(ConsoleNoiseClassifier.IsIgnorableForConsoleGate(msg), Is.True);
    }

    [Test]
    public void RobotoMono_font_error_is_project_owned_and_not_ignorable()
    {
        var msg = "Font 'RobotoMono-Regular' for character 'S' is missing from font data";
        Assert.That(ConsoleNoiseClassifier.Classify(msg), Is.EqualTo(ConsoleNoiseClassifier.NoiseClass.ProjectOwned));
        Assert.That(ConsoleNoiseClassifier.IsIgnorableForConsoleGate(msg), Is.False);
    }

    [Test]
    public void AudioListener_warning_is_project_owned_and_not_ignorable()
    {
        var msg =
            "There are 2 AudioListeners in the scene. Please ensure there is always exactly one AudioListener in the scene.";
        Assert.That(ConsoleNoiseClassifier.Classify(msg), Is.EqualTo(ConsoleNoiseClassifier.NoiseClass.ProjectOwned));
        Assert.That(ConsoleNoiseClassifier.IsIgnorableForConsoleGate(msg), Is.False);
    }

    [Test]
    public void Stack_from_IvanMurzak_MCP_is_package_tool()
    {
        var stack = "at com.IvanMurzak.Unity.MCP.Utils.UnityLogger:Log (...)";
        Assert.That(
            ConsoleNoiseClassifier.Classify("random warning", stack),
            Is.EqualTo(ConsoleNoiseClassifier.NoiseClass.PackageTool));
    }
}
