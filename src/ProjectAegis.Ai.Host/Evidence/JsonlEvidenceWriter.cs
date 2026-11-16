using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>
/// DRG-743: atomic JSONL evidence export via
/// <c>JsonSerializer.SerializeAsyncEnumerable(..., topLevelValues: true)</c>
/// (always LF / compact).
/// </summary>
public sealed class JsonlEvidenceWriter : IJsonlEvidenceWriter
{
    private static readonly byte[] Lf = [(byte)'\n'];

    /// <inheritdoc />
    public async Task<JsonlEvidenceWriteResult> WriteAsync(
        EvidenceDocument document,
        string artifactRoot,
        string fileName,
        bool enabled = true,
        int? maxEvidenceBytes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (fileName.IndexOfAny(['/', '\\', ':']) >= 0)
        {
            throw new ArgumentException("JSONL file name must be a single path segment.", nameof(fileName));
        }

        if (!enabled)
        {
            return new JsonlEvidenceWriteResult(
                Published: false,
                PublishedPath: null,
                ContentHashSha256: null,
                ByteLength: 0,
                PeakWorkingSetBytesDelta: 0,
                SkipReason: "disabled");
        }

        var limit = maxEvidenceBytes ?? 256 * 1024;
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEvidenceBytes));
        }

        Directory.CreateDirectory(artifactRoot);
        var publishedPath = Path.Combine(artifactRoot, fileName);
        var tempPath = Path.Combine(
            artifactRoot,
            "." + fileName + "." + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".tmp");

        // CMA-13: observational working-set high-water across write + validation (not GC heap delta).
        var process = Process.GetCurrentProcess();
        process.Refresh();
        var workingSetBaseline = process.WorkingSet64;
        var workingSetPeak = workingSetBaseline;

        void SampleWorkingSetPeak()
        {
            process.Refresh();
            var sample = process.WorkingSet64;
            if (sample > workingSetPeak)
            {
                workingSetPeak = sample;
            }
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                // Header as first JSONL line (PascalCase / declaration order via source-gen).
                await JsonSerializer.SerializeAsync(
                        stream,
                        document.Header,
                        EvidenceJsonlJsonContext.Default.EvidenceHeader,
                        cancellationToken)
                    .ConfigureAwait(false);
                await stream.WriteAsync(Lf, cancellationToken).ConfigureAwait(false);

                // Entries: SerializeAsyncEnumerable + topLevelValues => one JSON value per LF line.
                await JsonSerializer.SerializeAsyncEnumerable(
                        stream,
                        ToAsyncEnumerable(document.Entries, cancellationToken),
                        EvidenceJsonlJsonContext.Default.EvidenceEntry,
                        topLevelValues: true,
                        cancellationToken)
                    .ConfigureAwait(false);

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            SampleWorkingSetPeak();

            cancellationToken.ThrowIfCancellationRequested();

            var info = new FileInfo(tempPath);
            if (!info.Exists)
            {
                throw new InvalidOperationException("Temporary JSONL file missing after write.");
            }

            if (info.Length > limit)
            {
                throw new InvalidOperationException(
                    $"JSONL evidence exceeds MaxEvidenceBytes ({limit.ToString(CultureInfo.InvariantCulture)}).");
            }

            var hash = await HashFileSha256HexAsync(tempPath, cancellationToken).ConfigureAwait(false);
            SampleWorkingSetPeak();
            EnsureUnixNewlines(tempPath);
            SampleWorkingSetPeak();

            File.Move(tempPath, publishedPath);
            SampleWorkingSetPeak();

            var peakDelta = Math.Max(0, workingSetPeak - workingSetBaseline);

            return new JsonlEvidenceWriteResult(
                Published: true,
                PublishedPath: publishedPath,
                ContentHashSha256: hash,
                ByteLength: info.Length,
                PeakWorkingSetBytesDelta: peakDelta,
                SkipReason: null);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    /// <summary>
    /// Parses a JSONL file produced by this writer back into an <see cref="EvidenceDocument"/>.
    /// </summary>
    public static async Task<EvidenceDocument> ReadRoundTripAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        EvidenceHeader? header = null;
        var entries = new List<EvidenceEntry>();

        await foreach (var line in ReadLinesAsync(path, cancellationToken).ConfigureAwait(false))
        {
            if (line.Length == 0)
            {
                continue;
            }

            if (header is null)
            {
                header = JsonSerializer.Deserialize(
                    line,
                    EvidenceJsonlJsonContext.Default.EvidenceHeader)
                    ?? throw new InvalidOperationException("JSONL header line deserialized to null.");
                continue;
            }

            var entry = JsonSerializer.Deserialize(
                line,
                EvidenceJsonlJsonContext.Default.EvidenceEntry)
                ?? throw new InvalidOperationException("JSONL entry line deserialized to null.");
            entries.Add(entry);
        }

        if (header is null)
        {
            throw new InvalidOperationException("JSONL file had no header line.");
        }

        return new EvidenceDocument(header, entries);
    }

    private static async IAsyncEnumerable<EvidenceEntry> ToAsyncEnumerable(
        IReadOnlyList<EvidenceEntry> entries,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entries[i];
            await Task.Yield();
        }
    }

    private static async Task<string> HashFileSha256HexAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Defense-in-depth: reject CR so Windows/Linux hashes stay identical.
    /// SerializeAsyncEnumerable with topLevelValues already emits LF.
    /// </summary>
    private static void EnsureUnixNewlines(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.AsSpan().IndexOf((byte)'\r') >= 0)
        {
            throw new InvalidOperationException("JSONL evidence must use LF-only newlines.");
        }
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync(
        string path,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            yield return line;
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
            // best-effort
        }
        catch (UnauthorizedAccessException)
        {
            // best-effort
        }
    }
}
