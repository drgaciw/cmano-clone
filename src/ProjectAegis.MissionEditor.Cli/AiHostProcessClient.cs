using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.MissionEditor.Cli;

/// <summary>
/// Process-boundary IPC client for invoking the standalone <c>ProjectAegis.Ai.Host</c> executable
/// over stdin/stdout JSON using <see cref="ProjectAegis.Ai.Contracts"/> DTOs (DRG-772 / ADR-030).
/// </summary>
public sealed class AiHostProcessClient
{
    private const string HostAssemblyFileName = "ProjectAegis.Ai.Host.dll";
    private const string HostDllEnvVar = "AEGIS_AI_HOST_DLL";
    private const string DotnetHostEnvVar = "AEGIS_DOTNET_HOST";

    private static readonly JsonSerializerOptions IpcJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions CamelErrorOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly string? _explicitHostDllPath;
    private readonly string? _explicitDotnetPath;
    private readonly TimeSpan _timeout;

    /// <summary>Shared default process client instance (stateless).</summary>
    public static AiHostProcessClient Default { get; } = new();

    /// <summary>Creates a process-boundary client with optional deterministic host/runtime overrides.</summary>
    public AiHostProcessClient(
        string? hostDllPath = null,
        string? dotnetExecutablePath = null,
        TimeSpan? timeout = null)
    {
        _explicitHostDllPath = hostDllPath;
        _explicitDotnetPath = dotnetExecutablePath;
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>Invokes a verb on the out-of-process <c>ProjectAegis.Ai.Host</c> executable asynchronously.</summary>
    public async Task<AiHostIpcResponse> InvokeAsync<TRequest>(
        string verb,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verb);
        ArgumentNullException.ThrowIfNull(request);

        var hostDllPath = ResolveHostDllPath();
        if (hostDllPath is null)
        {
            return BuildClientError(
                "AI_HOST_UNAVAILABLE",
                "Could not locate ProjectAegis.Ai.Host.dll. Build ProjectAegis.Ai.Host or set AEGIS_AI_HOST_DLL.");
        }

        var dotnetPath = ResolveDotnetPath();
        var requestJson = JsonSerializer.Serialize(request, IpcJsonOptions);

        var psi = new ProcessStartInfo
        {
            FileName = dotnetPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add(hostDllPath);
        psi.ArgumentList.Add(verb);

        using var process = new Process();
        process.StartInfo = psi;
        try
        {
            if (!process.Start())
            {
                return BuildClientError("AI_HOST_UNAVAILABLE", "Failed to start ProjectAegis.Ai.Host process.");
            }
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return BuildClientError("AI_HOST_UNAVAILABLE", $"Failed to start ProjectAegis.Ai.Host via '{dotnetPath}': {ex.Message}");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            await process.StandardInput.WriteAsync(requestJson.AsMemory(), timeoutCts.Token).ConfigureAwait(false);
            await process.StandardInput.FlushAsync(timeoutCts.Token).ConfigureAwait(false);
            process.StandardInput.Close();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var detail = string.IsNullOrWhiteSpace(stderr)
                    ? $"ProjectAegis.Ai.Host exited with code {process.ExitCode}."
                    : stderr.Trim();
                return BuildClientError("AI_HOST_ERROR", detail);
            }

            var line = ExtractLastNonEmptyLine(stdout);
            if (string.IsNullOrWhiteSpace(line))
            {
                return BuildClientError("AI_HOST_ERROR", "ProjectAegis.Ai.Host produced an empty IPC response.");
            }

            var response = JsonSerializer.Deserialize<AiHostIpcResponse>(line, IpcJsonOptions);
            if (response is null || string.IsNullOrWhiteSpace(response.PayloadJson))
            {
                return BuildClientError("AI_HOST_ERROR", "ProjectAegis.Ai.Host returned a malformed IPC response frame.");
            }

            return response;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return BuildClientError("AI_HOST_TIMEOUT", $"ProjectAegis.Ai.Host timed out after {_timeout.TotalSeconds:0.#}s.");
        }
        catch (JsonException ex)
        {
            return BuildClientError("AI_HOST_ERROR", "Failed to parse ProjectAegis.Ai.Host IPC response: " + ex.Message);
        }
    }

    /// <summary>Synchronous wrapper over <see cref="InvokeAsync{TRequest}"/> for synchronous CLI verbs.</summary>
    public AiHostIpcResponse InvokeSync<TRequest>(string verb, TRequest request) =>
        InvokeAsync(verb, request).GetAwaiter().GetResult();

    /// <summary>
    /// Resolves <c>ProjectAegis.Ai.Host.dll</c> deterministically across Debug/Release build layouts
    /// without relying on global PATH.
    /// </summary>
    public string? ResolveHostDllPath()
    {
        if (_explicitHostDllPath is not null)
        {
            return File.Exists(_explicitHostDllPath) ? _explicitHostDllPath : null;
        }

        var envOverride = Environment.GetEnvironmentVariable(HostDllEnvVar);
        if (!string.IsNullOrWhiteSpace(envOverride))
        {
            return File.Exists(envOverride) ? envOverride : null;
        }

        var coLocated = Path.Combine(AppContext.BaseDirectory, HostAssemblyFileName);
        if (File.Exists(coLocated))
        {
            return coLocated;
        }

        var (activeConfig, activeTfm) = ResolveActiveBuildContext();
        var altConfig = string.Equals(activeConfig, "Release", StringComparison.OrdinalIgnoreCase)
            ? "Debug"
            : "Release";

        foreach (var root in EnumerateRepoRoots())
        {
            var preferred = Path.Combine(root, "src", "ProjectAegis.Ai.Host", "bin", activeConfig, activeTfm, HostAssemblyFileName);
            if (File.Exists(preferred))
            {
                return preferred;
            }

            var alt = Path.Combine(root, "src", "ProjectAegis.Ai.Host", "bin", altConfig, activeTfm, HostAssemblyFileName);
            if (File.Exists(alt))
            {
                return alt;
            }

            var fallbackTfm = Path.Combine(root, "src", "ProjectAegis.Ai.Host", "bin", activeConfig, "net11.0", HostAssemblyFileName);
            if (File.Exists(fallbackTfm))
            {
                return fallbackTfm;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the <c>dotnet</c> host binary deterministically without relying on global PATH.
    /// </summary>
    public string ResolveDotnetPath()
    {
        if (!string.IsNullOrWhiteSpace(_explicitDotnetPath))
        {
            return _explicitDotnetPath;
        }

        var explicitEnv = Environment.GetEnvironmentVariable(DotnetHostEnvVar);
        if (!string.IsNullOrWhiteSpace(explicitEnv) && File.Exists(explicitEnv))
        {
            return explicitEnv;
        }

        var dotnetHostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(dotnetHostPath) && File.Exists(dotnetHostPath))
        {
            return dotnetHostPath;
        }

        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var fileName = Path.GetFileName(processPath);
            if (string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                return processPath;
            }
        }

        var exeName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(dotnetRoot))
        {
            var rootCandidate = Path.Combine(dotnetRoot, exeName);
            if (File.Exists(rootCandidate))
            {
                return rootCandidate;
            }
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
        {
            var userDotnet = Path.Combine(home, ".dotnet", exeName);
            if (File.Exists(userDotnet))
            {
                return userDotnet;
            }
        }

        return "dotnet";
    }

    private static (string Configuration, string Tfm) ResolveActiveBuildContext()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var tfm = dir.Name;
        var configuration = dir.Parent?.Name;
        if (string.IsNullOrEmpty(configuration)
            || string.IsNullOrEmpty(tfm)
            || (!string.Equals(configuration, "Debug", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(configuration, "Release", StringComparison.OrdinalIgnoreCase)))
        {
            return ("Debug", "net11.0");
        }

        return (configuration, tfm);
    }

    private static IEnumerable<string> EnumerateRepoRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<string>();

        void WalkFrom(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
            {
                var hasHostProj = File.Exists(Path.Combine(dir.FullName, "src", "ProjectAegis.Ai.Host", "ProjectAegis.Ai.Host.csproj"));
                var hasSln = File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln"));
                if ((hasHostProj || hasSln) && seen.Add(dir.FullName))
                {
                    roots.Add(dir.FullName);
                }
            }
        }

        WalkFrom(AppContext.BaseDirectory);
        WalkFrom(Directory.GetCurrentDirectory());
        return roots;
    }

    private static string? ExtractLastNonEmptyLine(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return null;
        }

        using var reader = new StringReader(stdout);
        string? last = null;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                last = line;
            }
        }

        return last;
    }

    private static AiHostIpcResponse BuildClientError(string code, string message)
    {
        var payload = new Dictionary<string, object?>
        {
            ["ok"] = false,
            ["code"] = code,
            ["message"] = message,
        };
        return new AiHostIpcResponse(1, JsonSerializer.Serialize(payload, CamelErrorOptions));
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // already exited
        }
    }
}
