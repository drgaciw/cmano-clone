namespace ProjectAegis.Delegation.UnityAdapter.Save;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core;

public static class SaveEnvelopeBounds
{
    public const int MaxPayloadBytes = 64 * 1024 * 1024;
    public const int MaxJsonDepth = 16;
    public const int MaxJournalRows = 100_000;
    public const int MaxCheckpointRows = 50_000;
    public const int MaxPolicyCount = 256;
    public const int MaxShortStringLength = 4096;
    public const int MaxRawFingerprintLength = 2 * 1024 * 1024;
}

public static class SaveEnvelopeSerializer
{
    private static readonly HashSet<string> NetworkForbiddenKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "peer",
        "peers",
        "network",
        "networkMode",
        "network_mode",
        "multiplayer",
        "hotSeat",
        "hot_seat",
        "slotSide",
        "slot_side",
        "fogOfWarPerSide",
        "fog_of_war_per_side",
        "lockstep",
        "hostAddress",
        "host_address",
        "clientId",
        "client_id",
        "lobbyId",
        "lobby_id",
    };

    private static readonly HashSet<string> RootKeys = new(StringComparer.Ordinal)
    {
        "identity",
        "position",
        "inputJournal",
        "checksums",
        "envelopeHashSha256",
    };

    private static readonly HashSet<string> IdentityKeys = new(StringComparer.Ordinal)
    {
        "schemaVersion",
        "buildId",
        "scenarioId",
        "scenarioContentHash",
        "catalogSnapshotId",
        "wholeCatalogHash",
        "policyHashes",
        "globalSeed",
    };

    private static readonly HashSet<string> PolicyKeys = new(StringComparer.Ordinal)
    {
        "policyId",
        "policySha256",
    };

    private static readonly HashSet<string> PositionKeys = new(StringComparer.Ordinal)
    {
        "observedTick",
        "pipelineTick",
        "lastSequenceId",
    };

    private static readonly HashSet<string> InputJournalKeys = new(StringComparer.Ordinal)
    {
        "rows",
    };

    private static readonly HashSet<string> JournalRowKeys = new(StringComparer.Ordinal)
    {
        "sequenceOrder",
        "observedTick",
        "pipelineTick",
        "commandKind",
        "arguments",
    };

    private static readonly HashSet<string> JournalArgKeys = new(StringComparer.Ordinal)
    {
        "entityKeyValue",
        "targetId",
        "agentId",
        "orderKind",
        "riskLevel",
        "orderId",
        "subCommandId",
        "optionId",
        "resolvedSalvo",
        "simTime",
        "traits",
        "presetName",
        "autonomyLevel",
        "attentionBudget",
        "isFriendly",
        "roeLevelLabel",
        "explicitOverride",
        "accelerationFactor",
        "watchEventId",
        "contactId",
        "affiliation",
    };

    private static readonly HashSet<string> TraitKeys = new(StringComparer.Ordinal)
    {
        "aggression",
        "riskTolerance",
        "reactionDelay",
        "errorRate",
        "situationalAwareness",
        "decisiveness",
    };

    private static readonly HashSet<string> ChecksumKeys = new(StringComparer.Ordinal)
    {
        "journalDigest",
        "orderLogFingerprintSha256",
        "lastWorldHash",
        "combinedWorldHash",
        "checkpointRows",
    };

    private static readonly HashSet<string> CheckpointRowKeys = new(StringComparer.Ordinal)
    {
        "simTick",
        "worldHash",
        "logFingerprint",
        "lastSequenceId",
    };

    public static string ComputeJournalDigestSha256(IReadOnlyList<SaveJournalRow> rows)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartArray();
            for (var i = 0; i < rows.Count; i++)
            {
                WriteJournalRow(writer, rows[i]);
            }

            writer.WriteEndArray();
            writer.Flush();
        }

        return Sha256Hex(ms.ToArray());
    }

    public static string ComputeEnvelopeHashSha256(
        SaveIdentityBlock identity,
        SavePositionBlock position,
        SaveInputJournalBlock inputJournal,
        SaveChecksumsBlock checksums)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (position is null) throw new ArgumentNullException(nameof(position));
        if (inputJournal is null) throw new ArgumentNullException(nameof(inputJournal));
        if (checksums is null) throw new ArgumentNullException(nameof(checksums));

        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("identity");
            WriteIdentity(writer, identity);
            writer.WritePropertyName("position");
            WritePosition(writer, position);
            writer.WritePropertyName("inputJournal");
            WriteInputJournal(writer, inputJournal);
            writer.WritePropertyName("checksums");
            WriteChecksums(writer, checksums);
            writer.WriteEndObject();
            writer.Flush();
        }

        return Sha256Hex(ms.ToArray());
    }

    public static SaveEnvelope CreateSigned(
        SaveIdentityBlock identity,
        SavePositionBlock position,
        IReadOnlyList<SaveJournalRow> rows,
        string orderLogFingerprintSha256,
        ulong lastWorldHash,
        ulong combinedWorldHash,
        IReadOnlyList<SaveCheckpointRow> checkpointRows)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (position is null) throw new ArgumentNullException(nameof(position));
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        if (checkpointRows is null) throw new ArgumentNullException(nameof(checkpointRows));

        var sortedPolicies = identity.PolicyHashes
            .OrderBy(p => p.PolicyId, StringComparer.Ordinal)
            .ToArray();
        var canonicalIdentity = identity with { PolicyHashes = sortedPolicies };
        var journalBlock = new SaveInputJournalBlock(rows.ToArray());
        var journalDigest = ComputeJournalDigestSha256(journalBlock.Rows);
        var checksums = new SaveChecksumsBlock(
            journalDigest,
            orderLogFingerprintSha256,
            lastWorldHash,
            combinedWorldHash,
            checkpointRows.ToArray());
        var envelopeHash = ComputeEnvelopeHashSha256(canonicalIdentity, position, journalBlock, checksums);
        return new SaveEnvelope(canonicalIdentity, position, journalBlock, checksums, envelopeHash);
    }

    public static byte[] SerializeToUtf8Bytes(SaveEnvelope envelope)
    {
        if (envelope is null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("identity");
            WriteIdentity(writer, envelope.Identity);
            writer.WritePropertyName("position");
            WritePosition(writer, envelope.Position);
            writer.WritePropertyName("inputJournal");
            WriteInputJournal(writer, envelope.InputJournal);
            writer.WritePropertyName("checksums");
            WriteChecksums(writer, envelope.Checksums);
            writer.WriteString("envelopeHashSha256", envelope.EnvelopeHashSha256);
            writer.WriteEndObject();
            writer.Flush();
        }

        return ms.ToArray();
    }

    public static SaveResult<SaveEnvelope> TryDeserializeAndVerify(byte[]? utf8Bytes)
    {
        if (utf8Bytes is null)
        {
            return SaveResult<SaveEnvelope>.Fail(SaveFailureCode.TruncatedOrMalformedJson, "Save payload is null.");
        }

        return TryDeserializeAndVerify(utf8Bytes.AsSpan());
    }

    public static SaveResult<SaveEnvelope> TryDeserializeAndVerify(string? json)
    {
        if (json is null)
        {
            return SaveResult<SaveEnvelope>.Fail(SaveFailureCode.TruncatedOrMalformedJson, "Save JSON string is null.");
        }

        return TryDeserializeAndVerify(Encoding.UTF8.GetBytes(json));
    }

    public static SaveResult<SaveEnvelope> TryDeserializeAndVerify(ReadOnlySpan<byte> utf8Bytes)
    {
        if (utf8Bytes.Length == 0)
        {
            return SaveResult<SaveEnvelope>.Fail(SaveFailureCode.TruncatedOrMalformedJson, "Save payload is empty.");
        }

        if (utf8Bytes.Length > SaveEnvelopeBounds.MaxPayloadBytes)
        {
            return SaveResult<SaveEnvelope>.Fail(
                SaveFailureCode.PayloadTooLarge,
                $"Save payload size {utf8Bytes.Length} exceeds limit {SaveEnvelopeBounds.MaxPayloadBytes}.");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(
                utf8Bytes.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = SaveEnvelopeBounds.MaxJsonDepth,
                });
        }
        catch (JsonException ex)
        {
            var code = ex.Message.IndexOf("depth", StringComparison.OrdinalIgnoreCase) >= 0
                ? SaveFailureCode.DepthLimitExceeded
                : SaveFailureCode.TruncatedOrMalformedJson;
            return SaveResult<SaveEnvelope>.Fail(code, $"Malformed or truncated save JSON: {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return SaveResult<SaveEnvelope>.Fail(SaveFailureCode.TruncatedOrMalformedJson, "Root element must be a JSON object.");
            }

            if (!ValidateObjectKeys(root, RootKeys, "root", out var keyErrCode, out var keyErr))
            {
                return SaveResult<SaveEnvelope>.Fail(keyErrCode, keyErr!);
            }

            var idErrCode = SaveFailureCode.None;
            var posErrCode = SaveFailureCode.None;
            var jErrCode = SaveFailureCode.None;
            var chkErrCode = SaveFailureCode.None;
            if (!TryGetRequiredObject(root, "identity", out var idEl, out var err) ||
                !TryParseIdentity(idEl, out var identity, out idErrCode, out err) ||
                !TryGetRequiredObject(root, "position", out var posEl, out err) ||
                !TryParsePosition(posEl, out var position, out posErrCode, out err) ||
                !TryGetRequiredObject(root, "inputJournal", out var journalEl, out err) ||
                !TryParseJournal(journalEl, out var inputJournal, out jErrCode, out err) ||
                !TryGetRequiredObject(root, "checksums", out var chkEl, out err) ||
                !TryParseChecksums(chkEl, position.ObservedTick, out var checksums, out chkErrCode, out err))
            {
                var failureCode = idErrCode != SaveFailureCode.None ? idErrCode
                    : posErrCode != SaveFailureCode.None ? posErrCode
                    : jErrCode != SaveFailureCode.None ? jErrCode
                    : chkErrCode != SaveFailureCode.None ? chkErrCode
                    : SaveFailureCode.MissingRequiredField;
                return SaveResult<SaveEnvelope>.Fail(failureCode, err!);
            }

            if (!TryGetRequiredString(root, "envelopeHashSha256", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: false, out var envelopeHash, out err))
            {
                return SaveResult<SaveEnvelope>.Fail(SaveFailureCode.MissingRequiredField, err!);
            }

            var expectedJournalDigest = ComputeJournalDigestSha256(inputJournal.Rows);
            if (!string.Equals(checksums.JournalDigest, expectedJournalDigest, StringComparison.Ordinal))
            {
                return SaveResult<SaveEnvelope>.Fail(
                    SaveFailureCode.IntegrityMismatch,
                    "Save journal digest mismatch.");
            }

            var expectedEnvelopeHash = ComputeEnvelopeHashSha256(identity, position, inputJournal, checksums);
            if (!string.Equals(envelopeHash, expectedEnvelopeHash, StringComparison.Ordinal))
            {
                return SaveResult<SaveEnvelope>.Fail(
                    SaveFailureCode.IntegrityMismatch,
                    "Save envelope SHA-256 hash mismatch.");
            }

            return SaveResult<SaveEnvelope>.Ok(new SaveEnvelope(identity, position, inputJournal, checksums, envelopeHash));
        }
    }

    private static bool TryParseIdentity(
        JsonElement el,
        out SaveIdentityBlock identity,
        out SaveFailureCode code,
        out string? error)
    {
        identity = null!;
        code = SaveFailureCode.None;
        if (!ValidateObjectKeys(el, IdentityKeys, "identity", out code, out error))
        {
            return false;
        }

        if (!TryGetRequiredInt32(el, "schemaVersion", out var schemaVersion, out error))
        {
            code = SaveFailureCode.MissingRequiredField;
            return false;
        }

        if (schemaVersion != SaveEnvelope.CurrentSchemaVersion)
        {
            code = SaveFailureCode.UnknownSchemaVersion;
            error = $"Unsupported save schema version {schemaVersion}; expected {SaveEnvelope.CurrentSchemaVersion}.";
            return false;
        }

        if (!TryGetRequiredString(el, "buildId", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: false, out var buildId, out error) ||
            !TryGetRequiredString(el, "scenarioId", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: false, out var scenarioId, out error) ||
            !TryGetRequiredString(el, "scenarioContentHash", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: false, out var scenarioContentHash, out error) ||
            !TryGetRequiredString(el, "catalogSnapshotId", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: true, out var catalogSnapshotId, out error) ||
            !TryGetRequiredString(el, "wholeCatalogHash", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: true, out var wholeCatalogHash, out error) ||
            !TryGetRequiredInt32(el, "globalSeed", out var globalSeed, out error))
        {
            code = SaveFailureCode.MissingRequiredField;
            return false;
        }

        if (!el.TryGetProperty("policyHashes", out var policiesEl) || policiesEl.ValueKind != JsonValueKind.Array)
        {
            code = SaveFailureCode.MissingRequiredField;
            error = "identity.policyHashes must be an array.";
            return false;
        }

        var count = policiesEl.GetArrayLength();
        if (count > SaveEnvelopeBounds.MaxPolicyCount)
        {
            code = SaveFailureCode.PayloadTooLarge;
            error = $"identity.policyHashes count {count} exceeds limit {SaveEnvelopeBounds.MaxPolicyCount}.";
            return false;
        }

        var policies = new SavePolicyHashEntry[count];
        for (var i = 0; i < count; i++)
        {
            var pEl = policiesEl[i];
            if (pEl.ValueKind != JsonValueKind.Object)
            {
                code = SaveFailureCode.TruncatedOrMalformedJson;
                error = $"identity.policyHashes[{i}] must be an object.";
                return false;
            }

            if (!ValidateObjectKeys(pEl, PolicyKeys, $"identity.policyHashes[{i}]", out code, out error))
            {
                return false;
            }

            if (!TryGetRequiredString(pEl, "policyId", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: false, out var policyId, out error) ||
                !TryGetRequiredString(pEl, "policySha256", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: false, out var policySha, out error))
            {
                code = SaveFailureCode.MissingRequiredField;
                return false;
            }

            policies[i] = new SavePolicyHashEntry(policyId, policySha);
        }

        identity = new SaveIdentityBlock(
            schemaVersion,
            buildId,
            scenarioId,
            scenarioContentHash,
            catalogSnapshotId,
            wholeCatalogHash,
            policies,
            globalSeed);
        return true;
    }

    private static bool TryParsePosition(
        JsonElement el,
        out SavePositionBlock position,
        out SaveFailureCode code,
        out string? error)
    {
        position = null!;
        code = SaveFailureCode.None;
        if (!ValidateObjectKeys(el, PositionKeys, "position", out code, out error))
        {
            return false;
        }

        if (!TryGetRequiredUInt64(el, "observedTick", out var observedTick, out error) ||
            !TryGetRequiredUInt64(el, "pipelineTick", out var pipelineTick, out error) ||
            !TryGetRequiredUInt64(el, "lastSequenceId", out var lastSequenceId, out error))
        {
            code = SaveFailureCode.MissingRequiredField;
            return false;
        }

        position = new SavePositionBlock(observedTick, pipelineTick, lastSequenceId);
        return true;
    }

    private static bool TryParseJournal(
        JsonElement el,
        out SaveInputJournalBlock inputJournal,
        out SaveFailureCode code,
        out string? error)
    {
        inputJournal = null!;
        code = SaveFailureCode.None;
        if (!ValidateObjectKeys(el, InputJournalKeys, "inputJournal", out code, out error))
        {
            return false;
        }

        if (!el.TryGetProperty("rows", out var rowsEl) || rowsEl.ValueKind != JsonValueKind.Array)
        {
            code = SaveFailureCode.MissingRequiredField;
            error = "inputJournal.rows must be an array.";
            return false;
        }

        var count = rowsEl.GetArrayLength();
        if (count > SaveEnvelopeBounds.MaxJournalRows)
        {
            code = SaveFailureCode.PayloadTooLarge;
            error = $"inputJournal.rows count {count} exceeds limit {SaveEnvelopeBounds.MaxJournalRows}.";
            return false;
        }

        var rows = new SaveJournalRow[count];
        for (var i = 0; i < count; i++)
        {
            var rowEl = rowsEl[i];
            if (rowEl.ValueKind != JsonValueKind.Object)
            {
                code = SaveFailureCode.TruncatedOrMalformedJson;
                error = $"inputJournal.rows[{i}] must be an object.";
                return false;
            }

            if (!ValidateObjectKeys(rowEl, JournalRowKeys, $"inputJournal.rows[{i}]", out code, out error))
            {
                return false;
            }

            if (!TryGetRequiredUInt64(rowEl, "sequenceOrder", out var seqOrder, out error) ||
                !TryGetRequiredUInt64(rowEl, "observedTick", out var obsTick, out error) ||
                !TryGetRequiredUInt64(rowEl, "pipelineTick", out var pipeTick, out error) ||
                !TryGetRequiredString(rowEl, "commandKind", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: false, out var cmdKind, out error) ||
                !TryGetRequiredObject(rowEl, "arguments", out var argsEl, out error))
            {
                code = SaveFailureCode.MissingRequiredField;
                return false;
            }

            if (seqOrder != (ulong)i)
            {
                code = SaveFailureCode.IntegrityMismatch;
                error = $"inputJournal.rows[{i}].sequenceOrder={seqOrder} is out of sequence.";
                return false;
            }

            if (!TryParseJournalArgs(argsEl, i, out var args, out code, out error))
            {
                return false;
            }

            rows[i] = new SaveJournalRow(seqOrder, obsTick, pipeTick, cmdKind, args);
        }

        inputJournal = new SaveInputJournalBlock(rows);
        return true;
    }

    private static bool TryParseJournalArgs(
        JsonElement el,
        int rowIndex,
        out SaveJournalArguments args,
        out SaveFailureCode code,
        out string? error)
    {
        args = null!;
        code = SaveFailureCode.None;
        if (!ValidateObjectKeys(el, JournalArgKeys, $"inputJournal.rows[{rowIndex}].arguments", out code, out error))
        {
            return false;
        }

        if (!TryGetRequiredDouble(el, "simTime", out var simTime, out error))
        {
            code = SaveFailureCode.MissingRequiredField;
            return false;
        }

        if (!TryGetOptionalInt64(el, "entityKeyValue", out var entityKeyValue, out error) ||
            !TryGetOptionalString(el, "targetId", SaveEnvelopeBounds.MaxShortStringLength, out var targetId, out error) ||
            !TryGetOptionalString(el, "agentId", SaveEnvelopeBounds.MaxShortStringLength, out var agentId, out error) ||
            !TryGetOptionalInt32(el, "orderKind", out var orderKindRaw, out error) ||
            !TryGetOptionalInt32(el, "riskLevel", out var riskLevelRaw, out error) ||
            !TryGetOptionalInt64(el, "orderId", out var orderId, out error) ||
            !TryGetOptionalString(el, "subCommandId", SaveEnvelopeBounds.MaxShortStringLength, out var subCommandId, out error) ||
            !TryGetOptionalString(el, "optionId", SaveEnvelopeBounds.MaxShortStringLength, out var optionId, out error) ||
            !TryGetOptionalInt32(el, "resolvedSalvo", out var resolvedSalvo, out error) ||
            !TryGetOptionalString(el, "presetName", SaveEnvelopeBounds.MaxShortStringLength, out var presetName, out error) ||
            !TryGetOptionalInt32(el, "autonomyLevel", out var autonomyRaw, out error) ||
            !TryGetOptionalDouble(el, "attentionBudget", out var attentionBudget, out error) ||
            !TryGetOptionalBool(el, "isFriendly", out var isFriendly, out error) ||
            !TryGetOptionalString(el, "roeLevelLabel", SaveEnvelopeBounds.MaxShortStringLength, out var roeLabel, out error) ||
            !TryGetOptionalBool(el, "explicitOverride", out var explicitOverride, out error) ||
            !TryGetOptionalInt32(el, "accelerationFactor", out var accelFactor, out error) ||
            !TryGetOptionalString(el, "watchEventId", SaveEnvelopeBounds.MaxShortStringLength, out var watchEventId, out error) ||
            !TryGetOptionalString(el, "contactId", SaveEnvelopeBounds.MaxShortStringLength, out var contactId, out error) ||
            !TryGetOptionalString(el, "affiliation", SaveEnvelopeBounds.MaxShortStringLength, out var affiliation, out error))
        {
            code = SaveFailureCode.TruncatedOrMalformedJson;
            return false;
        }

        SaveTraitValues? traits = null;
        if (el.TryGetProperty("traits", out var traitsEl) && traitsEl.ValueKind != JsonValueKind.Null)
        {
            if (traitsEl.ValueKind != JsonValueKind.Object)
            {
                code = SaveFailureCode.TruncatedOrMalformedJson;
                error = "arguments.traits must be an object or null.";
                return false;
            }

            if (!ValidateObjectKeys(traitsEl, TraitKeys, "arguments.traits", out code, out error))
            {
                return false;
            }

            if (!TryGetRequiredDouble(traitsEl, "aggression", out var agg, out error) ||
                !TryGetRequiredDouble(traitsEl, "riskTolerance", out var riskTol, out error) ||
                !TryGetRequiredDouble(traitsEl, "reactionDelay", out var react, out error) ||
                !TryGetRequiredDouble(traitsEl, "errorRate", out var errRate, out error) ||
                !TryGetRequiredDouble(traitsEl, "situationalAwareness", out var sitAware, out error) ||
                !TryGetRequiredDouble(traitsEl, "decisiveness", out var dec, out error))
            {
                code = SaveFailureCode.MissingRequiredField;
                return false;
            }

            traits = new SaveTraitValues(agg, riskTol, react, errRate, sitAware, dec);
        }

        OrderKind? orderKind = orderKindRaw.HasValue ? (OrderKind)orderKindRaw.Value : null;
        RiskLevel? riskLevel = riskLevelRaw.HasValue ? (RiskLevel)riskLevelRaw.Value : null;
        AutonomyLevel? autonomyLevel = autonomyRaw.HasValue ? (AutonomyLevel)autonomyRaw.Value : null;

        args = new SaveJournalArguments(
            EntityKeyValue: entityKeyValue,
            TargetId: targetId,
            AgentId: agentId,
            OrderKind: orderKind,
            RiskLevel: riskLevel,
            OrderId: orderId,
            SubCommandId: subCommandId,
            OptionId: optionId,
            ResolvedSalvo: resolvedSalvo,
            SimTime: simTime,
            Traits: traits,
            PresetName: presetName,
            AutonomyLevel: autonomyLevel,
            AttentionBudget: attentionBudget,
            IsFriendly: isFriendly,
            RoeLevelLabel: roeLabel,
            ExplicitOverride: explicitOverride,
            AccelerationFactor: accelFactor,
            WatchEventId: watchEventId,
            ContactId: contactId,
            Affiliation: affiliation);
        return true;
    }

    private static bool TryParseChecksums(
        JsonElement el,
        ulong observedTick,
        out SaveChecksumsBlock checksums,
        out SaveFailureCode code,
        out string? error)
    {
        checksums = null!;
        code = SaveFailureCode.None;
        if (!ValidateObjectKeys(el, ChecksumKeys, "checksums", out code, out error))
        {
            return false;
        }

        if (!TryGetRequiredString(el, "journalDigest", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: false, out var journalDigest, out error) ||
            !TryGetRequiredString(el, "orderLogFingerprintSha256", SaveEnvelopeBounds.MaxShortStringLength, allowEmpty: false, out var orderLogFp, out error) ||
            !TryGetRequiredUInt64(el, "lastWorldHash", out var lastWorldHash, out error) ||
            !TryGetRequiredUInt64(el, "combinedWorldHash", out var combinedWorldHash, out error))
        {
            code = SaveFailureCode.MissingRequiredField;
            return false;
        }

        if (!el.TryGetProperty("checkpointRows", out var chkRowsEl) || chkRowsEl.ValueKind != JsonValueKind.Array)
        {
            code = SaveFailureCode.MissingRequiredField;
            error = "checksums.checkpointRows must be an array.";
            return false;
        }

        var count = chkRowsEl.GetArrayLength();
        if (count > SaveEnvelopeBounds.MaxCheckpointRows)
        {
            code = SaveFailureCode.PayloadTooLarge;
            error = $"checksums.checkpointRows count {count} exceeds limit {SaveEnvelopeBounds.MaxCheckpointRows}.";
            return false;
        }

        var rows = new SaveCheckpointRow[count];
        ulong prevTick = 0;
        for (var i = 0; i < count; i++)
        {
            var rEl = chkRowsEl[i];
            if (rEl.ValueKind != JsonValueKind.Object)
            {
                code = SaveFailureCode.TruncatedOrMalformedJson;
                error = $"checksums.checkpointRows[{i}] must be an object.";
                return false;
            }

            if (!ValidateObjectKeys(rEl, CheckpointRowKeys, $"checksums.checkpointRows[{i}]", out code, out error))
            {
                return false;
            }

            if (!TryGetRequiredUInt64(rEl, "simTick", out var simTick, out error) ||
                !TryGetRequiredUInt64(rEl, "worldHash", out var worldHash, out error) ||
                !TryGetRequiredString(rEl, "logFingerprint", SaveEnvelopeBounds.MaxRawFingerprintLength, allowEmpty: true, out var rawFp, out error) ||
                !TryGetRequiredUInt64(rEl, "lastSequenceId", out var seqId, out error))
            {
                code = SaveFailureCode.MissingRequiredField;
                return false;
            }

            if (simTick > observedTick)
            {
                code = SaveFailureCode.IntegrityMismatch;
                error = $"checksums.checkpointRows[{i}].simTick={simTick} exceeds observedTick={observedTick}.";
                return false;
            }

            if (i > 0 && simTick <= prevTick)
            {
                code = SaveFailureCode.IntegrityMismatch;
                error = $"checksums.checkpointRows[{i}].simTick={simTick} is not strictly increasing.";
                return false;
            }

            prevTick = simTick;
            rows[i] = new SaveCheckpointRow(simTick, worldHash, rawFp, seqId);
        }

        checksums = new SaveChecksumsBlock(journalDigest, orderLogFp, lastWorldHash, combinedWorldHash, rows);
        return true;
    }

    private static bool ValidateObjectKeys(
        JsonElement obj,
        HashSet<string> allowedKeys,
        string path,
        out SaveFailureCode code,
        out string? error)
    {
        code = SaveFailureCode.None;
        error = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var prop in obj.EnumerateObject())
        {
            if (NetworkForbiddenKeys.Contains(prop.Name) ||
                prop.Name.IndexOf("multiplayer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("hotseat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("lockstep", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                code = SaveFailureCode.UnsupportedNetworkField;
                error = $"Unsupported multiplayer/network field '{path}.{prop.Name}' in H6 save envelope.";
                return false;
            }

            if (!seen.Add(prop.Name))
            {
                code = SaveFailureCode.DuplicateOrUnknownField;
                error = $"Duplicate field '{path}.{prop.Name}' in save envelope.";
                return false;
            }

            if (!allowedKeys.Contains(prop.Name))
            {
                code = SaveFailureCode.DuplicateOrUnknownField;
                error = $"Unknown field '{path}.{prop.Name}' in save envelope.";
                return false;
            }
        }

        return true;
    }

    private static bool TryGetRequiredObject(JsonElement obj, string name, out JsonElement value, out string? error)
    {
        if (!obj.TryGetProperty(name, out value) || value.ValueKind != JsonValueKind.Object)
        {
            error = $"Missing or invalid object property '{name}'.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryGetRequiredString(
        JsonElement obj,
        string name,
        int maxLength,
        bool allowEmpty,
        out string value,
        out string? error)
    {
        value = string.Empty;
        if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            error = $"Missing or non-string property '{name}'.";
            return false;
        }

        var s = prop.GetString() ?? string.Empty;
        if (s.Length > maxLength)
        {
            error = $"Property '{name}' exceeds max length {maxLength}.";
            return false;
        }

        if (!allowEmpty && string.IsNullOrEmpty(s))
        {
            error = $"Property '{name}' must be non-empty.";
            return false;
        }

        value = s;
        error = null;
        return true;
    }

    private static bool TryGetOptionalString(
        JsonElement obj,
        string name,
        int maxLength,
        out string? value,
        out string? error)
    {
        value = null;
        error = null;
        if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (prop.ValueKind != JsonValueKind.String)
        {
            error = $"Property '{name}' must be a string or null.";
            return false;
        }

        var s = prop.GetString();
        if (s != null && s.Length > maxLength)
        {
            error = $"Property '{name}' exceeds max length {maxLength}.";
            return false;
        }

        value = s;
        return true;
    }

    private static bool TryGetRequiredInt32(JsonElement obj, string name, out int value, out string? error)
    {
        value = 0;
        if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt32(out value))
        {
            error = $"Missing or invalid Int32 property '{name}'.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryGetOptionalInt32(JsonElement obj, string name, out int? value, out string? error)
    {
        value = null;
        error = null;
        if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt32(out var parsed))
        {
            error = $"Property '{name}' must be an Int32 or null.";
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryGetOptionalInt64(JsonElement obj, string name, out long? value, out string? error)
    {
        value = null;
        error = null;
        if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt64(out var parsed))
        {
            error = $"Property '{name}' must be an Int64 or null.";
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryGetRequiredUInt64(JsonElement obj, string name, out ulong value, out string? error)
    {
        value = 0;
        if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Number || !prop.TryGetUInt64(out value))
        {
            error = $"Missing or invalid UInt64 property '{name}'.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryGetRequiredDouble(JsonElement obj, string name, out double value, out string? error)
    {
        value = 0;
        if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Number || !prop.TryGetDouble(out value) || double.IsNaN(value) || double.IsInfinity(value))
        {
            error = $"Missing or invalid finite Double property '{name}'.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryGetOptionalDouble(JsonElement obj, string name, out double? value, out string? error)
    {
        value = null;
        error = null;
        if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetDouble(out var parsed) || double.IsNaN(parsed) || double.IsInfinity(parsed))
        {
            error = $"Property '{name}' must be a finite Double or null.";
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryGetOptionalBool(JsonElement obj, string name, out bool? value, out string? error)
    {
        value = null;
        error = null;
        if (!obj.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (prop.ValueKind != JsonValueKind.True && prop.ValueKind != JsonValueKind.False)
        {
            error = $"Property '{name}' must be a Boolean or null.";
            return false;
        }

        value = prop.GetBoolean();
        return true;
    }

    private static void WriteIdentity(Utf8JsonWriter writer, SaveIdentityBlock identity)
    {
        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", identity.SchemaVersion);
        writer.WriteString("buildId", identity.BuildId);
        writer.WriteString("scenarioId", identity.ScenarioId);
        writer.WriteString("scenarioContentHash", identity.ScenarioContentHash);
        writer.WriteString("catalogSnapshotId", identity.CatalogSnapshotId);
        writer.WriteString("wholeCatalogHash", identity.WholeCatalogHash);
        writer.WriteStartArray("policyHashes");
        foreach (var policy in identity.PolicyHashes)
        {
            writer.WriteStartObject();
            writer.WriteString("policyId", policy.PolicyId);
            writer.WriteString("policySha256", policy.PolicySha256);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteNumber("globalSeed", identity.GlobalSeed);
        writer.WriteEndObject();
    }

    private static void WritePosition(Utf8JsonWriter writer, SavePositionBlock position)
    {
        writer.WriteStartObject();
        writer.WriteNumber("observedTick", position.ObservedTick);
        writer.WriteNumber("pipelineTick", position.PipelineTick);
        writer.WriteNumber("lastSequenceId", position.LastSequenceId);
        writer.WriteEndObject();
    }

    private static void WriteInputJournal(Utf8JsonWriter writer, SaveInputJournalBlock journal)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("rows");
        for (var i = 0; i < journal.Rows.Count; i++)
        {
            WriteJournalRow(writer, journal.Rows[i]);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteJournalRow(Utf8JsonWriter writer, SaveJournalRow row)
    {
        writer.WriteStartObject();
        writer.WriteNumber("sequenceOrder", row.SequenceOrder);
        writer.WriteNumber("observedTick", row.ObservedTick);
        writer.WriteNumber("pipelineTick", row.PipelineTick);
        writer.WriteString("commandKind", row.CommandKind);
        writer.WritePropertyName("arguments");
        WriteJournalArguments(writer, row.Arguments);
        writer.WriteEndObject();
    }

    private static void WriteJournalArguments(Utf8JsonWriter writer, SaveJournalArguments args)
    {
        writer.WriteStartObject();
        WriteNullableNumber(writer, "entityKeyValue", args.EntityKeyValue);
        WriteNullableString(writer, "targetId", args.TargetId);
        WriteNullableString(writer, "agentId", args.AgentId);
        WriteNullableNumber(writer, "orderKind", args.OrderKind.HasValue ? (int)args.OrderKind.Value : null);
        WriteNullableNumber(writer, "riskLevel", args.RiskLevel.HasValue ? (int)args.RiskLevel.Value : null);
        WriteNullableNumber(writer, "orderId", args.OrderId);
        WriteNullableString(writer, "subCommandId", args.SubCommandId);
        WriteNullableString(writer, "optionId", args.OptionId);
        WriteNullableNumber(writer, "resolvedSalvo", args.ResolvedSalvo);
        writer.WriteNumber("simTime", args.SimTime == 0.0 ? 0.0 : args.SimTime);
        if (args.Traits is { } t)
        {
            writer.WriteStartObject("traits");
            writer.WriteNumber("aggression", t.Aggression == 0.0 ? 0.0 : t.Aggression);
            writer.WriteNumber("riskTolerance", t.RiskTolerance == 0.0 ? 0.0 : t.RiskTolerance);
            writer.WriteNumber("reactionDelay", t.ReactionDelay == 0.0 ? 0.0 : t.ReactionDelay);
            writer.WriteNumber("errorRate", t.ErrorRate == 0.0 ? 0.0 : t.ErrorRate);
            writer.WriteNumber("situationalAwareness", t.SituationalAwareness == 0.0 ? 0.0 : t.SituationalAwareness);
            writer.WriteNumber("decisiveness", t.Decisiveness == 0.0 ? 0.0 : t.Decisiveness);
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteNull("traits");
        }

        WriteNullableString(writer, "presetName", args.PresetName);
        WriteNullableNumber(writer, "autonomyLevel", args.AutonomyLevel.HasValue ? (int)args.AutonomyLevel.Value : null);
        WriteNullableDouble(writer, "attentionBudget", args.AttentionBudget);
        WriteNullableBool(writer, "isFriendly", args.IsFriendly);
        WriteNullableString(writer, "roeLevelLabel", args.RoeLevelLabel);
        WriteNullableBool(writer, "explicitOverride", args.ExplicitOverride);
        WriteNullableNumber(writer, "accelerationFactor", args.AccelerationFactor);
        WriteNullableString(writer, "watchEventId", args.WatchEventId);
        WriteNullableString(writer, "contactId", args.ContactId);
        WriteNullableString(writer, "affiliation", args.Affiliation);
        writer.WriteEndObject();
    }

    private static void WriteChecksums(Utf8JsonWriter writer, SaveChecksumsBlock checksums)
    {
        writer.WriteStartObject();
        writer.WriteString("journalDigest", checksums.JournalDigest);
        writer.WriteString("orderLogFingerprintSha256", checksums.OrderLogFingerprintSha256);
        writer.WriteNumber("lastWorldHash", checksums.LastWorldHash);
        writer.WriteNumber("combinedWorldHash", checksums.CombinedWorldHash);
        writer.WriteStartArray("checkpointRows");
        for (var i = 0; i < checksums.CheckpointRows.Count; i++)
        {
            var c = checksums.CheckpointRows[i];
            writer.WriteStartObject();
            writer.WriteNumber("simTick", c.SimTick);
            writer.WriteNumber("worldHash", c.WorldHash);
            writer.WriteString("logFingerprint", c.LogFingerprint);
            writer.WriteNumber("lastSequenceId", c.LastSequenceId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null) writer.WriteNull(name);
        else writer.WriteString(name, value);
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string name, long? value)
    {
        if (value is { } v) writer.WriteNumber(name, v);
        else writer.WriteNull(name);
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string name, int? value)
    {
        if (value is { } v) writer.WriteNumber(name, v);
        else writer.WriteNull(name);
    }

    private static void WriteNullableDouble(Utf8JsonWriter writer, string name, double? value)
    {
        if (value is { } v) writer.WriteNumber(name, v == 0.0 ? 0.0 : v);
        else writer.WriteNull(name);
    }

    private static void WriteNullableBool(Utf8JsonWriter writer, string name, bool? value)
    {
        if (value is { } v) writer.WriteBoolean(name, v);
        else writer.WriteNull(name);
    }

    private static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            var b = hash[i];
            chars[i * 2] = GetHexNibble(b >> 4);
            chars[i * 2 + 1] = GetHexNibble(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) =>
        (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}
