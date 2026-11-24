using System.ComponentModel;
using System.Diagnostics;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Packaging;

/// <summary>
/// UAT D20 guard: every tracked folder under <c>unity/ProjectAegis/Assets/UI</c> must have its
/// folder <c>.meta</c> tracked in git. Unity regenerates missing metas on disk, so a disk-based
/// check can never fail; this asks git (the index) instead. An untracked folder meta makes each
/// clone mint a fresh folder GUID.
/// </summary>
public sealed class UnityFolderMetaTrackedTests
{
    private const string UiRoot = "unity/ProjectAegis/Assets/UI";

    [Test]
    public void Every_tracked_ui_folder_has_a_tracked_folder_meta()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var tracked = GitLsFiles(repoRoot!, UiRoot + "/");
        if (tracked == null)
        {
            Assert.Inconclusive("git is unavailable or this is not a git work tree; cannot verify tracked folder metas.");
            return;
        }

        var trackedSet = new HashSet<string>(tracked, StringComparer.Ordinal);
        var folders = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in tracked)
        {
            var dir = file.Contains('/') ? file[..file.LastIndexOf('/')] : string.Empty;
            while (dir.Length > UiRoot.Length && dir.StartsWith(UiRoot + "/", StringComparison.Ordinal))
            {
                folders.Add(dir);
                dir = dir[..dir.LastIndexOf('/')];
            }
        }

        Assume.That(folders, Is.Not.Empty, "No tracked folders found under " + UiRoot);

        var missing = folders.Where(d => !trackedSet.Contains(d + ".meta")).Select(d => d + ".meta").ToList();
        Assert.That(
            missing,
            Is.Empty,
            "Tracked UI folders without a tracked folder .meta (each clone would mint a new GUID):\n" + string.Join("\n", missing));
    }

    private static List<string>? GitLsFiles(string repoRoot, string pathspec)
    {
        try
        {
            var psi = new ProcessStartInfo("git")
            {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("ls-files");
            psi.ArgumentList.Add("-z");
            psi.ArgumentList.Add("--");
            psi.ArgumentList.Add(pathspec);

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return null;
            }

            var stderrTask = proc.StandardError.ReadToEndAsync();
            var stdout = proc.StandardOutput.ReadToEnd();
            if (!proc.WaitForExit(30000))
            {
                try { proc.Kill(); } catch (InvalidOperationException) { }
                return null;
            }

            stderrTask.Wait();
            if (proc.ExitCode != 0)
            {
                return null;
            }

            return stdout.Split('\0', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}
