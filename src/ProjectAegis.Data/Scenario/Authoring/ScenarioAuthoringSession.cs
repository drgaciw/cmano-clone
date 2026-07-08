namespace ProjectAegis.Data.Scenario.Authoring;

using ProjectAegis.Data.Validation;

/// <summary>
/// File-backed authoring session: dirty flag, path, and command bus.
/// <see cref="EditorState"/> is derived-only (camera/layers) and must never be fed to the Validation Engine.
/// </summary>
public sealed class ScenarioAuthoringSession : IDisposable
{
    private ScenarioAuthoringSession(string path, ScenarioDocumentEditor editor)
    {
        Path = path;
        Editor = editor;
        Bus = new ScenarioEditCommandBus(this);
    }

    /// <summary>Absolute or relative path of the open scenario document.</summary>
    public string Path { get; }

    /// <summary>Mutable document editor for the open scenario.</summary>
    public ScenarioDocumentEditor Editor { get; }

    /// <summary>Serialized mutation bus (editVersion → undo → mutate → commit).</summary>
    public ScenarioEditCommandBus Bus { get; }

    /// <summary>True when in-memory state has diverged from the last successful <see cref="Save"/>.</summary>
    public bool IsDirty { get; internal set; }

    /// <summary>Current optimistic concurrency token (<see cref="ScenarioMetadataDto.EditVersion"/>).</summary>
    public int EditVersion => Editor.Metadata.EditVersion;

    /// <summary>Derived-only camera/layers; never pass to validation or headless sim entry points.</summary>
    public ScenarioEditorStateDto EditorState { get; set; } = new();

    /// <summary>Opens an existing scenario file. Throws <see cref="FileNotFoundException"/> if missing.</summary>
    public static ScenarioAuthoringSession Open(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Scenario not found", path);
        }

        return new ScenarioAuthoringSession(path, ScenarioDocumentEditor.Load(path));
    }

    /// <summary>Persists the editor document to <see cref="Path"/> and clears the dirty flag.</summary>
    public void Save()
    {
        Editor.Save(Path);
        IsDirty = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No unmanaged resources; IDisposable for session lifetime / using-block clarity.
    }
}

/// <summary>Result of a single scenario mutation command (place/move/clone/mission attach, etc.).</summary>
public sealed class ScenarioMutationResult
{
    /// <summary>True when the mutation committed successfully.</summary>
    public bool Ok { get; init; }

    /// <summary>Machine-readable error code (e.g. <c>CONFLICT</c>, <c>INVALID_OPERATION</c>).</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Human-readable error detail when <see cref="Ok"/> is false.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Edit version after the attempt (current on conflict; bumped on success).</summary>
    public int EditVersion { get; init; }

    /// <summary>Content hash of the document after a successful mutation, or on conflict if available.</summary>
    public string? FileHash { get; init; }

    /// <summary>Live validation report snapshot after a successful mutation.</summary>
    public ValidationReport? Report { get; init; }
}
