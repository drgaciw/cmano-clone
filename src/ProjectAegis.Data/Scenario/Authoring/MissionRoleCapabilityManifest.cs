namespace ProjectAegis.Data.Scenario.Authoring;

using Validation;

/// <summary>Whether an execution backend runs an authored mission role (DRG-345 / proposed AME-6.11).</summary>
public enum MissionRoleExecution
{
    /// <summary>The backend generates every order the role requires.</summary>
    Executed = 0,

    /// <summary>The role is known but the backend does not generate the orders it requires.</summary>
    NotExecuted = 1,

    /// <summary>The role or the backend is not described by the manifest.</summary>
    Unknown = 2,
}

/// <summary>One manifest row: an authored mission role and its runtime support on a backend.</summary>
public sealed class MissionRoleCapability
{
    /// <summary>Authored mission type (<c>Patrol</c>, <c>Strike</c>, <c>Ferry</c>, <c>Support</c>).</summary>
    public string MissionType { get; init; } = "";

    /// <summary>Support role (<c>Tanker</c>, <c>AEW</c>, <c>EW</c>); null for non-Support types.</summary>
    public string? SupportRole { get; init; }

    /// <summary>Execution backend this row describes.</summary>
    public string BackendId { get; init; } = "";

    /// <summary>Runtime support of the role on <see cref="BackendId"/>.</summary>
    public MissionRoleExecution Execution { get; init; }

    /// <summary>
    /// Delegation <c>OrderKind</c> names the role needs the backend to generate. Names that are not
    /// an <c>OrderKind</c> (e.g. <c>Refuel</c>) document behaviour no runtime implements.
    /// </summary>
    public IReadOnlyList<string> RequiredOrderKinds { get; init; } = Array.Empty<string>();

    /// <summary>Runtime symbol(s) that back the <see cref="Execution"/> claim.</summary>
    public string RuntimeEvidence { get; init; } = "";
}

/// <summary>Capability verdict for one authored mission.</summary>
public sealed class MissionRoleCapabilityAssessment
{
    /// <summary>Authored mission id.</summary>
    public string MissionId { get; init; } = "";

    /// <summary>Authored mission type, trimmed.</summary>
    public string MissionType { get; init; } = "";

    /// <summary>Authored support role, trimmed; null when absent.</summary>
    public string? SupportRole { get; init; }

    /// <summary>Backend the mission was assessed against.</summary>
    public string BackendId { get; init; } = "";

    /// <summary>Runtime support verdict.</summary>
    public MissionRoleExecution Execution { get; init; }

    /// <summary>Stable finding code when <see cref="Execution"/> is not <see cref="MissionRoleExecution.Executed"/>.</summary>
    public string? FindingCode { get; init; }

    /// <summary>User-facing disclosure (also the finding message when one is emitted).</summary>
    public string Message { get; init; } = "";
}

/// <summary>
/// Single headless source of truth for which authored mission roles the Play / simulate backend
/// executes (DRG-345 / proposed AME-6.11). Export, Play, CLI and GUI all read this manifest; none of
/// them decide runtime support on their own.
///
/// The only backend reachable from Export/Play today is <see cref="DefaultBackendId"/>
/// (<c>BalticReplayHarness</c> + <c>SimulationSession</c>), whose agents choose among the
/// <c>PatrolCandidateEngagePolicy.Candidates</c> intents <c>Hold</c>, <c>Move</c> and <c>Engage</c>.
/// A role is <see cref="MissionRoleExecution.Executed"/> only when all its
/// <see cref="MissionRoleCapability.RequiredOrderKinds"/> are in that set; the UnityAdapter test suite
/// re-derives this against the runtime policy so the manifest cannot drift from what runs.
/// </summary>
public static class MissionRoleCapabilityManifest
{
    /// <summary>Headless Play / simulate backend id.</summary>
    public const string DefaultBackendId = "baltic-replay-harness";

    /// <summary>Known role that the backend does not execute.</summary>
    public const string CodeRoleNotExecuted = "MISSION_ROLE_NOT_EXECUTED";

    /// <summary>Mission type / support role the manifest does not describe.</summary>
    public const string CodeRoleUnknown = "MISSION_ROLE_UNKNOWN";

    /// <summary>Selected execution backend is not described by the manifest.</summary>
    public const string CodeBackendUnknown = "MISSION_EXECUTION_BACKEND_UNKNOWN";

    private const string HarnessEvidence =
        "BalticReplayHarness agents use PatrolCandidateEngagePolicy (Hold, Move, Engage) with MvpEngagementResolver";

    /// <summary>Manifest rows ordered by mission type then support role (ordinal).</summary>
    public static IReadOnlyList<MissionRoleCapability> Entries { get; } =
    [
        Row("Ferry", null, MissionRoleExecution.NotExecuted, ["ReturnToBase"],
            HarnessEvidence + "; no policy generates ReturnToBase/rebase to a destination base"),
        Row("Patrol", null, MissionRoleExecution.Executed, ["Hold", "Move"], HarnessEvidence),
        Row("Strike", null, MissionRoleExecution.Executed, ["Engage"], HarnessEvidence),
        Row("Support", "AEW", MissionRoleExecution.NotExecuted, ["SetSensors"],
            HarnessEvidence + "; no policy generates SetSensors for an airborne early-warning station"),
        Row("Support", "EW", MissionRoleExecution.NotExecuted, ["SetEwPosture"],
            HarnessEvidence + "; no policy generates SetEwPosture for a support station"),
        Row("Support", "Tanker", MissionRoleExecution.NotExecuted, ["Refuel"],
            HarnessEvidence + "; no refuelling order or runtime exists"),
    ];

    private static readonly string SupportedRolesList = string.Join(
        ", ",
        Entries.Where(e => e.Execution == MissionRoleExecution.Executed)
            .Select(e => e.SupportRole == null ? e.MissionType : e.MissionType + "/" + e.SupportRole));

    /// <summary>True when <paramref name="code"/> is one of the capability finding codes.</summary>
    public static bool IsCapabilityCode(string? code) =>
        string.Equals(code, CodeRoleNotExecuted, StringComparison.Ordinal)
        || string.Equals(code, CodeRoleUnknown, StringComparison.Ordinal)
        || string.Equals(code, CodeBackendUnknown, StringComparison.Ordinal);

    /// <summary>Looks up the manifest row for a type/role on a backend (case- and whitespace-insensitive).</summary>
    public static MissionRoleCapability? Find(string? missionType, string? supportRole, string backendId = DefaultBackendId)
    {
        var type = Normalize(missionType);
        var role = Normalize(supportRole);
        return Entries.FirstOrDefault(e =>
            string.Equals(e.BackendId, backendId, StringComparison.Ordinal)
            && string.Equals(e.MissionType, type, StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.SupportRole, role, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Assesses one authored mission against <paramref name="backendId"/>.</summary>
    public static MissionRoleCapabilityAssessment Assess(ScenarioMissionDto mission, string backendId = DefaultBackendId)
    {
        if (mission == null)
        {
            throw new ArgumentNullException(nameof(mission));
        }

        var type = Normalize(mission.Type) ?? "";
        var role = Normalize(mission.SupportRole);
        var roleText = role == null ? $"{type} mission '{mission.Id}'" : $"{type} mission '{mission.Id}' role '{role}'";
        const string Blocked = "Export and Play are blocked; Save remains available.";

        if (!Entries.Any(e => string.Equals(e.BackendId, backendId, StringComparison.Ordinal)))
        {
            return Build(mission.Id, type, role, backendId, MissionRoleExecution.Unknown, CodeBackendUnknown,
                $"{roleText} cannot be assessed: execution backend '{backendId}' is unknown. {Blocked} Select backend '{DefaultBackendId}'.");
        }

        // Support missions are keyed by role; a role on a non-Support type is ignored by the runtime.
        var lookupRole = string.Equals(type, "Support", StringComparison.OrdinalIgnoreCase) ? role : null;
        if (lookupRole == null && string.Equals(type, "Support", StringComparison.OrdinalIgnoreCase))
        {
            return Build(mission.Id, type, role, backendId, MissionRoleExecution.Unknown, CodeRoleUnknown,
                $"{roleText} has no support role. {Blocked} Set a support role or change the mission to a supported role ({SupportedRolesList}).");
        }

        var entry = Find(type, lookupRole, backendId);
        if (entry == null)
        {
            var label = type.Length == 0 ? $"Mission '{mission.Id}' has no type and" : $"{roleText} is unknown to backend '{backendId}' and";
            return Build(mission.Id, type, role, backendId, MissionRoleExecution.Unknown, CodeRoleUnknown,
                $"{label} cannot run. {Blocked} Change it to a supported role ({SupportedRolesList}).");
        }

        if (entry.Execution == MissionRoleExecution.Executed)
        {
            return Build(mission.Id, entry.MissionType, role, backendId, MissionRoleExecution.Executed, null,
                $"{roleText} is executed by backend '{backendId}'.");
        }

        return Build(mission.Id, type, role, backendId, MissionRoleExecution.NotExecuted, CodeRoleNotExecuted,
            $"{roleText} is not executed by backend '{backendId}'. {Blocked} Remove the mission or change it to a supported role ({SupportedRolesList}).");
    }

    /// <summary>Assesses every mission in <paramref name="document"/>, ordered by mission id (ordinal).</summary>
    public static IReadOnlyList<MissionRoleCapabilityAssessment> AssessDocument(
        ScenarioDocumentDto document,
        string backendId = DefaultBackendId)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return (document.Missions ?? Array.Empty<ScenarioMissionDto>())
            .Select(m => Assess(m, backendId))
            .OrderBy(a => a.MissionId, StringComparer.Ordinal)
            .ThenBy(a => a.MissionType, StringComparer.Ordinal)
            .ThenBy(a => a.SupportRole ?? "", StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Error-severity findings for every mission the backend does not execute, ordered by mission id.
    /// Executed missions produce no finding, so supported scenarios keep their existing report hashes.
    /// </summary>
    public static IReadOnlyList<ValidationFinding> EvaluateFindings(
        ScenarioDocumentDto document,
        string backendId = DefaultBackendId) =>
        AssessDocument(document, backendId)
            .Where(a => a.FindingCode != null)
            .Select(a => new ValidationFinding(
                a.FindingCode!,
                ValidationSeverity.Error,
                a.Message,
                MissionId: a.MissionId,
                Data: FindingData(a)))
            .ToArray();

    private static IReadOnlyDictionary<string, string> FindingData(MissionRoleCapabilityAssessment a)
    {
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["backend"] = a.BackendId,
            ["execution"] = a.Execution.ToString(),
            ["missionType"] = a.MissionType,
        };
        if (a.SupportRole != null)
        {
            data["supportRole"] = a.SupportRole;
        }

        return data;
    }

    private static MissionRoleCapability Row(
        string type,
        string? role,
        MissionRoleExecution execution,
        string[] requiredOrderKinds,
        string evidence) =>
        new()
        {
            MissionType = type,
            SupportRole = role,
            BackendId = DefaultBackendId,
            Execution = execution,
            RequiredOrderKinds = requiredOrderKinds,
            RuntimeEvidence = evidence,
        };

    private static MissionRoleCapabilityAssessment Build(
        string missionId,
        string type,
        string? role,
        string backendId,
        MissionRoleExecution execution,
        string? code,
        string message) =>
        new()
        {
            MissionId = missionId,
            MissionType = type,
            SupportRole = role,
            BackendId = backendId,
            Execution = execution,
            FindingCode = code,
            Message = message,
        };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
}
