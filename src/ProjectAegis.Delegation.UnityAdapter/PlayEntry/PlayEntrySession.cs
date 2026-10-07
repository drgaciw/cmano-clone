namespace ProjectAegis.Delegation.UnityAdapter.PlayEntry;

using System.Text.Json;
using Data.Catalog;
using Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using Controllers;
using Core;
using Orchestration;
using Targets;
using Traits;
using Bridge;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Headless play-entry façade (S123 DRG-243 / DRG-246): scenario package list + load into Planning,
/// staged mode and side selection, gated Begin Execution, and Reset to Planning.
/// Owns no world truth (ADR-010): each load or reset builds a fresh <see cref="DelegationBridge"/>, and the
/// selected mode is applied through <see cref="DelegationBridge.ConfigureSimulationMode"/> only at Begin,
/// because the AgentVsAgent configuration begins execution immediately.
/// </summary>
public sealed class PlayEntrySession
{
    private static readonly PlaySide[] CommandFriendlyOnly = { PlaySide.Friendly };
    private static readonly PlaySide[] BothSides = { PlaySide.Friendly, PlaySide.Opposing };

    private readonly bool _mvpEngagement;
    private readonly ICatalogReader? _catalog;
    private string? _sourcePath;
    private SimulationModeKind? _mode;
    private PlaySide? _side;
    private int _generation;
    private IReadOnlyList<PlayCommandedTarget> _commanded = Array.Empty<PlayCommandedTarget>();

    /// <param name="mvpEngagement">Bind the MVP engage session on each bridge (Unity host default).</param>
    /// <param name="catalog">Optional catalog reader forwarded to each bridge.</param>
    public PlayEntrySession(bool mvpEngagement = true, ICatalogReader? catalog = null)
    {
        _mvpEngagement = mvpEngagement;
        _catalog = catalog;
    }

    /// <summary>Active bridge; replaced on successful load / reset, null until the first load.</summary>
    public DelegationBridge? Bridge { get; private set; }

    public ScenarioPackage? Package { get; private set; }

    /// <summary>Loaded scenario document (briefing source); null until the first load.</summary>
    public ScenarioDocumentDto? Document { get; private set; }

    public PlayEntryState State => Package == null
        ? PlayEntryState.Empty
        : new PlayEntryState(
            Package.ScenarioId,
            Package.PolicyId,
            _sourcePath,
            Bridge?.Phase,
            _mode,
            _side,
            _generation);

    /// <summary>
    /// Targets the player commands, captured at a successful Begin (those left human-controlled by the mode
    /// configuration). Empty before Begin, in AgentVsAgent, and after load / reset.
    /// </summary>
    public IReadOnlyList<PlayCommandedTarget> CommandedTargets => _commanded;

    /// <summary>Scenario package browse list (CMD-27 library rows with pre-load feasibility).</summary>
    public static IReadOnlyList<ScenarioLibraryEntry> ListPackages(string? scenariosDir = null)
    {
        var dir = scenariosDir ?? ScenarioDataPaths.TryResolveScenariosDirectory();
        return dir == null
            ? Array.Empty<ScenarioLibraryEntry>()
            : ScenarioLibraryLister.ListFromDirectory(dir);
    }

    /// <summary>Sides offered after a mode is chosen; Human mode always commands the friendly side.</summary>
    public static IReadOnlyList<PlaySide> OfferedSides(SimulationModeKind mode) => mode switch
    {
        SimulationModeKind.Human => CommandFriendlyOnly,
        SimulationModeKind.Mixed => BothSides,
        SimulationModeKind.AgentVsAgent => BothSides,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown simulation mode."),
    };

    /// <summary>Load a library row. Unavailable rows fail with their pre-load reason; state is unchanged.</summary>
    public PlayEntryResult TryLoad(ScenarioLibraryEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (!entry.Available)
        {
            return PlayEntryResult.Fail(
                PlayEntryErrorCodes.PackageUnavailable,
                $"Scenario '{entry.ScenarioId}' is unavailable: {entry.UnavailableReason ?? "unknown reason"}.");
        }

        return TryLoadFromPath(entry.SourcePath);
    }

    /// <summary>
    /// Resolve a scenario document into a package + Planning-phase bridge. Every failure returns an
    /// explicit error and leaves the prior package, bridge, mode and side untouched.
    /// </summary>
    public PlayEntryResult TryLoadFromPath(string scenarioPath)
    {
        if (string.IsNullOrWhiteSpace(scenarioPath) || !File.Exists(scenarioPath))
        {
            return PlayEntryResult.Fail(
                PlayEntryErrorCodes.FileUnreadable,
                $"Scenario file not found: {scenarioPath}");
        }

        ScenarioDocumentDto document;
        try
        {
            document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return PlayEntryResult.Fail(
                PlayEntryErrorCodes.FileUnreadable,
                $"Scenario file unreadable: {scenarioPath} ({ex.Message})");
        }
        catch (Exception ex)
        {
            return PlayEntryResult.Fail(
                PlayEntryErrorCodes.SchemaError,
                $"Scenario document invalid: {scenarioPath} ({ex.Message})");
        }

        // Explicit null deserializes without a loader exception, then FromDocument
        // dereferences Metadata.PolicyId. A missing key keeps the DTO initializer and
        // would otherwise load with package defaults. Both are schema refusals.
        if (document.Metadata is null || !DeclaresMetadataObject(scenarioPath))
        {
            return PlayEntryResult.Fail(
                PlayEntryErrorCodes.SchemaError,
                $"Scenario document invalid: {scenarioPath} (metadata is required).");
        }

        var scenarioId = ScenarioLibraryProjection.ScenarioIdFromPath(scenarioPath);
        var package = ScenarioPackage.FromDocument(scenarioId, document, _catalog);
        if (ScenarioPolicyRepository.TryGet(package.PolicyId) == null)
        {
            return PlayEntryResult.Fail(
                PlayEntryErrorCodes.PolicyUnresolved,
                $"Scenario '{scenarioId}' references unknown policy '{package.PolicyId}'.");
        }

        if (!TryBuildBridge(package, out var bridge, out var failure))
        {
            return failure!;
        }

        Package = package;
        Document = document;
        Bridge = bridge;
        _sourcePath = scenarioPath;
        _mode = null;
        _side = null;
        _commanded = Array.Empty<PlayCommandedTarget>();
        _generation++;
        return PlayEntryResult.Ok($"Loaded '{scenarioId}' (policy {package.PolicyId}) into Planning.");
    }

    /// <summary>Stage a simulation mode (Planning only). Drops a staged side the new mode does not offer.</summary>
    public PlayEntryResult TrySelectMode(SimulationModeKind mode)
    {
        var blocked = RequirePlanning();
        if (blocked != null)
        {
            return blocked;
        }

        var offered = OfferedSides(mode);
        _mode = mode;
        if (_side.HasValue && !offered.Contains(_side.Value))
        {
            _side = null;
        }

        return PlayEntryResult.Ok($"Mode {mode} selected.");
    }

    /// <summary>Stage the play side (Planning only, after a mode is selected).</summary>
    public PlayEntryResult TrySelectSide(PlaySide side)
    {
        var blocked = RequirePlanning();
        if (blocked != null)
        {
            return blocked;
        }

        if (!_mode.HasValue)
        {
            return PlayEntryResult.Fail(PlayEntryErrorCodes.ModeRequired, "Select a simulation mode before picking a side.");
        }

        if (!OfferedSides(_mode.Value).Contains(side))
        {
            return PlayEntryResult.Fail(
                PlayEntryErrorCodes.SideNotOffered,
                $"Side {side} is not offered in {_mode.Value} mode.");
        }

        _side = side;
        return PlayEntryResult.Ok($"Side {side} selected.");
    }

    public BeginExecutionGate EvaluateBeginGate()
    {
        if (Package == null || Bridge == null)
        {
            return new BeginExecutionGate(new[] { PlayEntryErrorCodes.NoPackage });
        }

        if (Bridge.Phase != SimulationPhase.Planning)
        {
            return new BeginExecutionGate(new[] { PlayEntryErrorCodes.NotPlanning });
        }

        var reasons = new List<string>(2);
        if (!_mode.HasValue)
        {
            reasons.Add(PlayEntryErrorCodes.ModeRequired);
        }

        if (!_side.HasValue)
        {
            reasons.Add(PlayEntryErrorCodes.SideRequired);
        }

        return new BeginExecutionGate(reasons);
    }

    /// <summary>
    /// Apply the staged mode + side via <see cref="DelegationBridge.ConfigureSimulationMode"/> and begin execution.
    /// Rejected (no bridge mutation) unless both a mode and a side are selected in Planning.
    /// </summary>
    public PlayEntryResult TryBeginExecution(
        IReadOnlyList<ICommandableTarget> friendly,
        IReadOnlyList<ICommandableTarget> opposing,
        TraitVector? defaultTraits = null,
        AutonomyLevel agentAutonomy = AutonomyLevel.FullAutonomous)
    {
        if (friendly == null)
        {
            throw new ArgumentNullException(nameof(friendly));
        }

        if (opposing == null)
        {
            throw new ArgumentNullException(nameof(opposing));
        }

        var gate = EvaluateBeginGate();
        if (!gate.CanBegin)
        {
            var code = gate.BlockedReasons[0];
            return PlayEntryResult.Fail(code, $"Begin Execution blocked: {string.Join(", ", gate.BlockedReasons)}.");
        }

        var mode = _mode!.Value;
        var side = _side!.Value;
        Bridge!.ConfigureSimulationMode(
            new SimulationModeProfile(mode, PlayerControlsFriendlySide: side == PlaySide.Friendly),
            friendly,
            opposing,
            defaultTraits ?? PersonalityCatalog.All[0].Traits,
            agentAutonomy);
        Bridge.BeginExecution();
        _commanded = CollectHumanControlled(friendly, opposing);
        return PlayEntryResult.Ok($"Executing in {mode} mode as {side}.");
    }

    private static IReadOnlyList<PlayCommandedTarget> CollectHumanControlled(
        IReadOnlyList<ICommandableTarget> friendly,
        IReadOnlyList<ICommandableTarget> opposing)
    {
        var commanded = new List<PlayCommandedTarget>(friendly.Count + opposing.Count);
        foreach (var target in friendly)
        {
            if (target.Slot.Active is HumanController)
            {
                commanded.Add(new PlayCommandedTarget(target, PlaySide.Friendly));
            }
        }

        foreach (var target in opposing)
        {
            if (target.Slot.Active is HumanController)
            {
                commanded.Add(new PlayCommandedTarget(target, PlaySide.Opposing));
            }
        }

        return commanded;
    }

    /// <summary>
    /// Rebuild the bridge from the loaded package and return to Planning with mode and side cleared.
    /// Hosts must re-register entities on the new <see cref="Bridge"/>.
    /// </summary>
    public PlayEntryResult TryResetToPlanning()
    {
        if (Package == null)
        {
            return PlayEntryResult.Fail(PlayEntryErrorCodes.NoPackage, "No scenario package loaded.");
        }

        if (!TryBuildBridge(Package, out var bridge, out var failure))
        {
            return failure!;
        }

        Bridge = bridge;
        _mode = null;
        _side = null;
        _commanded = Array.Empty<PlayCommandedTarget>();
        _generation++;
        return PlayEntryResult.Ok($"Reset '{Package.ScenarioId}' to Planning.");
    }

    /// <summary>
    /// True when the file's root object has a <c>metadata</c> property whose value is an object.
    /// Null and a missing key are both refusals; an empty object is present and may use package defaults.
    /// </summary>
    private static bool DeclaresMetadataObject(string scenarioPath)
    {
        try
        {
            using var stream = File.OpenRead(scenarioPath);
            using var json = JsonDocument.Parse(
                stream,
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });
            if (json.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            JsonElement? metadata = null;
            foreach (var property in json.RootElement.EnumerateObject())
            {
                if (property.Name.Equals("metadata", StringComparison.OrdinalIgnoreCase))
                {
                    metadata = property.Value;
                }
            }

            return metadata is { ValueKind: JsonValueKind.Object };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private PlayEntryResult? RequirePlanning()
    {
        if (Package == null || Bridge == null)
        {
            return PlayEntryResult.Fail(PlayEntryErrorCodes.NoPackage, "No scenario package loaded.");
        }

        return Bridge.Phase == SimulationPhase.Planning
            ? null
            : PlayEntryResult.Fail(PlayEntryErrorCodes.NotPlanning, "Mode and side are locked once execution begins.");
    }

    private bool TryBuildBridge(ScenarioPackage package, out DelegationBridge? bridge, out PlayEntryResult? failure)
    {
        bridge = null;
        failure = null;
        if (package.Seed > int.MaxValue)
        {
            failure = PlayEntryResult.Fail(
                PlayEntryErrorCodes.SeedUnsupported,
                $"Scenario '{package.ScenarioId}' seed {package.Seed} exceeds the bridge seed range (max {int.MaxValue}).");
            return false;
        }

        try
        {
            bridge = new DelegationBridge(
                (int)package.Seed,
                mvpEngagement: _mvpEngagement,
                scenarioPolicyId: package.PolicyId,
                catalog: _catalog);
            return true;
        }
        catch (Exception ex)
        {
            failure = PlayEntryResult.Fail(
                PlayEntryErrorCodes.BridgeBuildFailed,
                $"Scenario '{package.ScenarioId}' could not start: {ex.Message}");
            return false;
        }
    }
}
