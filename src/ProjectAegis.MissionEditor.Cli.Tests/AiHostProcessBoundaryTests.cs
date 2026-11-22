using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using NUnit.Framework;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// DRG-772: enforces that <c>ProjectAegis.MissionEditor.Cli</c> does not reference
/// <c>ProjectAegis.Ai.Host</c> or <c>Microsoft.Extensions.AI</c> in-process and invokes
/// <c>ProjectAegis.Ai.Host</c> across a clean process boundary.
/// </summary>
[TestFixture]
public sealed class AiHostProcessBoundaryTests
{
    [Test]
    public void MissionEditor_cli_csproj_and_assembly_do_not_reference_ai_host_or_ms_extensions_ai()
    {
        var csprojPath = ResolveRepoFile("src", "ProjectAegis.MissionEditor.Cli", "ProjectAegis.MissionEditor.Cli.csproj");
        Assert.That(csprojPath, Is.Not.Null, "Could not locate ProjectAegis.MissionEditor.Cli.csproj.");

        var xml = XDocument.Load(csprojPath!);
        var projectRefs = xml
            .Descendants("ProjectReference")
            .Select(el => Path.GetFileName((el.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/')))
            .ToArray();

        Assert.That(
            projectRefs,
            Does.Not.Contain("ProjectAegis.Ai.Host.csproj"),
            "DRG-772: MissionEditor.Cli.csproj must not hold a ProjectReference to ProjectAegis.Ai.Host.csproj.");

        var referencedAssemblies = typeof(AarExplainCommand).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        Assert.That(
            referencedAssemblies,
            Does.Not.Contain("ProjectAegis.Ai.Host"),
            "DRG-772: MissionEditor.Cli assembly must not reference ProjectAegis.Ai.Host in-process.");
        Assert.That(
            referencedAssemblies.Where(n => n.StartsWith("Microsoft.Extensions.AI", StringComparison.OrdinalIgnoreCase)),
            Is.Empty,
            "DRG-772: MissionEditor.Cli assembly must not pull in Microsoft.Extensions.AI.");

        var cliOutputDir = Path.GetDirectoryName(typeof(AarExplainCommand).Assembly.Location);
        Assert.That(cliOutputDir, Is.Not.Null);
        var loadedNames = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name ?? string.Empty)
            .ToArray();
        Assert.That(
            loadedNames,
            Does.Not.Contain("ProjectAegis.Ai.Host"),
            "DRG-772: ProjectAegis.Ai.Host must execute out-of-process, never loaded into the CLI test AppDomain.");
    }

    [Test]
    public void Default_process_client_resolves_ai_host_dll_and_dotnet_deterministically()
    {
        var hostDll = AiHostProcessClient.Default.ResolveHostDllPath();
        Assert.That(hostDll, Is.Not.Null, "Expected built ProjectAegis.Ai.Host.dll to be discoverable.");
        Assert.That(File.Exists(hostDll), Is.True);

        var dotnetPath = AiHostProcessClient.Default.ResolveDotnetPath();
        Assert.That(string.IsNullOrWhiteSpace(dotnetPath), Is.False);
    }

    [Test]
    public async Task Missing_ai_host_binary_returns_structured_unavailable_error()
    {
        var evidence = ResolveRepoFile("tests", "ai-fixtures", "aar-explain", "evidence.jsonl");
        Assert.That(evidence, Is.Not.Null);

        var brokenClient = new AiHostProcessClient(hostDllPath: Path.Combine(Path.GetTempPath(), "nonexistent-ai-host.dll"));
        var sb = new StringBuilder();
        await using var writer = new StringWriter(sb);

        var exit = await AarExplainCommand.RunAsync(
            evidence,
            enabled: false,
            artifactRoot: null,
            scriptPath: null,
            writer,
            brokenClient);

        Assert.That(exit, Is.EqualTo(1));
        using var doc = JsonDocument.Parse(sb.ToString());
        Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.False);
        Assert.That(doc.RootElement.GetProperty("code").GetString(), Is.EqualTo("AI_HOST_UNAVAILABLE"));
    }

    [Test]
    public void Missing_ai_host_binary_for_mission_plan_suggest_returns_structured_unavailable_error()
    {
        var brokenClient = new AiHostProcessClient(hostDllPath: Path.Combine(Path.GetTempPath(), "nonexistent-ai-host.dll"));
        using var writer = new StringWriter();

        var exit = MissionPlanSuggestCommand.RunProviderFake(
            intent: "Blue patrol u1",
            fixtureCaseId: "valid-draft",
            scriptPath: null,
            expectedEditVersion: null,
            currentEditVersion: null,
            allowedUnitIds: null,
            allowedTargetIds: null,
            supportedMissionTypes: null,
            idempotencyKey: null,
            dbRef: null,
            output: writer,
            hostClient: brokenClient);

        Assert.That(exit, Is.EqualTo(1));
        using var doc = JsonDocument.Parse(writer.ToString());
        Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.False);
        Assert.That(doc.RootElement.GetProperty("code").GetString(), Is.EqualTo("AI_HOST_UNAVAILABLE"));
    }

    [Test]
    public async Task Process_client_rejects_unknown_verb_cleanly()
    {
        var response = await AiHostProcessClient.Default.InvokeAsync(
            "unknown_verb_drg772",
            new AiHostAarExplainRequest(null, false, null, null));

        Assert.That(response.ExitCode, Is.EqualTo(1));
        using var doc = JsonDocument.Parse(response.PayloadJson);
        Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.False);
        Assert.That(doc.RootElement.GetProperty("code").GetString(), Is.EqualTo("AI_HOST_ERROR"));
    }

    private static string? ResolveRepoFile(params string[] relativeSegments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
