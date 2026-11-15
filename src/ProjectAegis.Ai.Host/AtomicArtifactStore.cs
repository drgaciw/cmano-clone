using System.Text;
using System.Text.Json;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host;

/// <summary>
/// Publishes <see cref="ArtifactEnvelope"/> JSON via temp write + validate + atomic rename.
/// Cancelled or failed writes leave no published file.
/// </summary>
public sealed class AtomicArtifactStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _rootDirectory;
    private readonly AiHostOptions _options;

    /// <summary>Creates a store rooted at <paramref name="rootDirectory"/>.</summary>
    public AtomicArtifactStore(string rootDirectory, AiHostOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        ArgumentNullException.ThrowIfNull(options);
        _rootDirectory = rootDirectory;
        _options = options;
        Directory.CreateDirectory(_rootDirectory);
    }

    /// <summary>Root directory for published artifacts.</summary>
    public string RootDirectory => _rootDirectory;

    /// <summary>
    /// Serializes and publishes <paramref name="envelope"/> to <paramref name="fileName"/> under the root.
    /// </summary>
    public async Task PublishAsync(
        string fileName,
        ArtifactEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(envelope);
        if (fileName.IndexOfAny(['/', '\\', ':']) >= 0)
        {
            throw new ArgumentException("Artifact file name must be a single path segment.", nameof(fileName));
        }

        var publishedPath = Path.Combine(_rootDirectory, fileName);
        var tempPath = Path.Combine(_rootDirectory, "." + fileName + "." + Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var json = JsonSerializer.Serialize(envelope, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            if (bytes.Length > _options.MaxEvidenceBytes)
            {
                throw new InvalidOperationException(
                    $"Artifact exceeds MaxEvidenceBytes ({_options.MaxEvidenceBytes}).");
            }

            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            ValidateTempFile(tempPath, bytes.Length);

            // Atomic publish on the same volume: replace if present is not required for Wave 0.
            File.Move(tempPath, publishedPath);
        }
        catch
        {
            TryDelete(tempPath);
            // Published path must remain absent on failure/cancel (do not leave a partial publish).
            if (!File.Exists(publishedPath))
            {
                // ok
            }

            throw;
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    /// <summary>Returns true when the published artifact exists.</summary>
    public bool Exists(string fileName) => File.Exists(Path.Combine(_rootDirectory, fileName));

    private static void ValidateTempFile(string tempPath, int expectedLength)
    {
        var info = new FileInfo(tempPath);
        if (!info.Exists || info.Length != expectedLength)
        {
            throw new InvalidOperationException("Temporary artifact failed size validation.");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // best-effort cleanup
        }
        catch (UnauthorizedAccessException)
        {
            // best-effort cleanup
        }
    }
}
