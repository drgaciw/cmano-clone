namespace ProjectAegis.Delegation.ThreatAssessment;

using System.Text.Json;

/// <summary>Explicit authoring-time loader; callers load once and inject the result into ThreatAssessmentInput.Tuning.</summary>
public static class ThreatAssessmentPolicyLoader
{
    /// <summary>
    /// Reads the optional threatAssessmentUrgency section of a scenario policy.
    /// Omitted settings retain compatibility defaults; confidence tuning remains unchanged.
    /// No file access occurs in the projector or simulation tick.
    /// </summary>
    public static ThreatAssessmentTuning LoadFromScenarioPolicy(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Advisory tuning requires a scenario policy object with an id.");
            }

            JsonElement? id = null;
            foreach (var property in root.EnumerateObject())
                if (string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase)) id = property.Value;
            if (id is null || id.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(id.Value.GetString()))
                throw new InvalidDataException("Advisory tuning requires a scenario policy object with an id.");

            var tuning = ThreatUrgencyTuning.Default;
            var sections = root.EnumerateObject().Where(property => property.NameEquals("threatAssessmentUrgency")).ToArray();
            if (sections.Length == 0) return ThreatAssessmentTuning.Default;
            if (sections.Length != 1 || sections[0].Value.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("threatAssessmentUrgency must be a single object.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in sections[0].Value.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw new InvalidDataException($"Duplicate advisory tuning setting: {property.Name}.");
                if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetDouble(out var value)
                    || !double.IsFinite(value) || value < 0 || value > 1)
                    throw new InvalidDataException($"Advisory tuning {property.Name} must be finite and in [0, 1].");

                tuning = property.Name switch
                {
                    "inZoneBase" => tuning with { InZoneBase = value },
                    "approachingBase" => tuning with { ApproachingBase = value },
                    "outOfZoneBase" => tuning with { OutOfZoneBase = value },
                    "defensiveBonus" => tuning with { DefensiveBonus = value },
                    "hypersonicBonus" => tuning with { HypersonicBonus = value },
                    "missingFireControlPenalty" => tuning with { MissingFireControlPenalty = value },
                    "immediateFloor" => tuning with { ImmediateFloor = value },
                    "focusFloor" => tuning with { FocusFloor = value },
                    "watchFloor" => tuning with { WatchFloor = value },
                    "lowFloor" => tuning with { LowFloor = value },
                    "lowConfidenceFloor" => tuning with { LowConfidenceFloor = value },
                    _ => throw new InvalidDataException($"Unknown advisory tuning setting: {property.Name}."),
                };
            }

            if (tuning.LowFloor > tuning.WatchFloor || tuning.WatchFloor > tuning.FocusFloor
                || tuning.FocusFloor > tuning.ImmediateFloor)
                throw new InvalidDataException("Advisory band floors must be ordered low <= watch <= focus <= immediate.");

            return ThreatAssessmentTuning.Default with { Urgency = tuning };
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Invalid scenario policy JSON: {path}.", exception);
        }
    }
}
