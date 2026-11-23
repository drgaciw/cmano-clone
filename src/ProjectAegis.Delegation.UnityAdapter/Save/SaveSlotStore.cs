namespace ProjectAegis.Delegation.UnityAdapter.Save;

/// <summary>
/// Fixed 3-manual + 1-rolling-autosave slot store (ADR-029 / h6-save-resume-contract.md).
/// Writes via temp file + WriteThrough + Flush(true) + atomic move/replace so failed or
/// partial writes preserve prior slot contents.
/// </summary>
public sealed class SaveSlotStore
{
    public const string Manual1 = "manual-1";
    public const string Manual2 = "manual-2";
    public const string Manual3 = "manual-3";
    public const string Autosave = "autosave";

    private static readonly HashSet<string> AllowedSlots = new(StringComparer.Ordinal)
    {
        Manual1,
        Manual2,
        Manual3,
        Autosave,
    };

    private readonly string _rootDirectory;

    public SaveSlotStore(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory must be non-empty.", nameof(rootDirectory));
        }

        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    public string RootDirectory => _rootDirectory;

    public bool IsValidSlotId(string? slotId) =>
        slotId is not null && AllowedSlots.Contains(slotId);

    public string? TryGetSlotPath(string? slotId) =>
        IsValidSlotId(slotId)
            ? Path.Combine(_rootDirectory, slotId + ".aegis-save.json")
            : null;

    public SaveOperationResult Save(
        string slotId,
        SaveEnvelope envelope,
        Func<(bool Ok, string? Violation)>? preWriteGuard = null,
        Action<string>? faultInjector = null)
    {
        if (envelope is null)
        {
            return SaveOperationResult.Fail(SaveFailureCode.InvalidArgument, "SaveEnvelope is null.");
        }

        var slotPath = TryGetSlotPath(slotId);
        if (slotPath is null)
        {
            return SaveOperationResult.Fail(SaveFailureCode.InvalidSlotId, $"Unsupported save slot '{slotId}'.");
        }

        if (preWriteGuard != null)
        {
            var (ok, violation) = preWriteGuard();
            if (!ok)
            {
                return SaveOperationResult.Fail(
                    SaveFailureCode.UnjournaledMutationDetected,
                    violation ?? "Unjournaled authority mutation detected prior to save.");
            }
        }

        byte[] utf8Bytes;
        try
        {
            utf8Bytes = SaveEnvelopeSerializer.SerializeToUtf8Bytes(envelope);
        }
        catch (Exception ex)
        {
            return SaveOperationResult.Fail(SaveFailureCode.WriteFailed, $"Failed to serialize save envelope: {ex.Message}");
        }

        var verify = SaveEnvelopeSerializer.TryDeserializeAndVerify(utf8Bytes);
        if (!verify.Succeeded)
        {
            return SaveOperationResult.Fail(verify.FailureCode, verify.FailureReason ?? "Envelope self-verification failed.");
        }

        Directory.CreateDirectory(_rootDirectory);
        var tempPath = slotPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var backupPath = slotPath + ".bak." + Guid.NewGuid().ToString("N");

        try
        {
            using (var fs = new FileStream(
                       tempPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       4096,
                       FileOptions.WriteThrough))
            {
                fs.Write(utf8Bytes, 0, utf8Bytes.Length);
                faultInjector?.Invoke(tempPath);
                fs.Flush(flushToDisk: true);
            }

            if (!File.Exists(slotPath))
            {
                File.Move(tempPath, slotPath);
            }
            else
            {
                File.Replace(tempPath, slotPath, backupPath, ignoreMetadataErrors: true);
                TryDeleteFile(backupPath);
            }

            return SaveOperationResult.Ok();
        }
        catch (Exception ex)
        {
            TryDeleteFile(tempPath);
            TryDeleteFile(backupPath);
            return SaveOperationResult.Fail(SaveFailureCode.WriteFailed, $"Atomic slot write failed: {ex.Message}");
        }
    }

    public SaveResult<SaveEnvelope> Load(string slotId)
    {
        var slotPath = TryGetSlotPath(slotId);
        if (slotPath is null)
        {
            return SaveResult<SaveEnvelope>.Fail(SaveFailureCode.InvalidSlotId, $"Unsupported save slot '{slotId}'.");
        }

        if (!File.Exists(slotPath))
        {
            return SaveResult<SaveEnvelope>.Fail(SaveFailureCode.SlotNotFound, $"Save slot '{slotId}' is empty.");
        }

        try
        {
            var info = new FileInfo(slotPath);
            if (info.Length > SaveEnvelopeBounds.MaxPayloadBytes)
            {
                return SaveResult<SaveEnvelope>.Fail(
                    SaveFailureCode.PayloadTooLarge,
                    $"Save slot '{slotId}' size {info.Length} exceeds limit {SaveEnvelopeBounds.MaxPayloadBytes}.");
            }

            var bytes = File.ReadAllBytes(slotPath);
            return SaveEnvelopeSerializer.TryDeserializeAndVerify(bytes);
        }
        catch (Exception ex)
        {
            return SaveResult<SaveEnvelope>.Fail(SaveFailureCode.TruncatedOrMalformedJson, $"Failed reading slot '{slotId}': {ex.Message}");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup of temp/backup files.
        }
    }
}
