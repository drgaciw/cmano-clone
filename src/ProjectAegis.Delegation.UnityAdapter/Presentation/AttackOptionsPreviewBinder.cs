using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-262 / REQ-14 ENG-02: attack-options menu preview bound from the engage-options projection.
/// Reads <see cref="EngageAttackOptions"/> rows only (ADR-010 §2–3, ADR-007, ADR-001).
/// Does not resolve orders or consult a second fact source.
/// </summary>
public sealed record AttackOptionsPreviewState(
    string PreviewLine,
    IReadOnlyList<AttackOptionMenuRow> Rows,
    string CueClass,
    string Fingerprint)
{
    /// <summary>Cleared menu when the projection has no options.</summary>
    public static AttackOptionsPreviewState Empty { get; } = new(
        "ATTACK: —",
        Array.Empty<AttackOptionMenuRow>(),
        AttackOptionsCueClasses.Empty,
        "atk:empty");
}

/// <summary>One attack-menu row: projection fact plus the button name the host already wires.</summary>
public sealed record AttackOptionMenuRow(
    string OptionId,
    string? ButtonName,
    string Label,
    bool Enabled,
    string? AbortReason,
    string ButtonText,
    string CueClass);

/// <summary>Non-color USS cue tokens for attack-option rows (text + border class).</summary>
public static class AttackOptionsCueClasses
{
    public const string Empty = "attack-option-cue--empty";
    public const string Ready = "attack-option-cue--ready";
    public const string Blocked = "attack-option-cue--blocked";

    /// <summary>All cue classes hosts must clear before applying the active row cue.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Empty, Ready, Blocked };
}

/// <summary>
/// Maps an existing engage-options menu onto the preview line and button rows.
/// Button names come from <see cref="AttackMenuPanelBinder"/>; option facts come from the menu.
/// </summary>
public static class AttackOptionsPreviewBinder
{
    /// <summary>UXML element name for the attack-options preview line.</summary>
    public const string PreviewElementName = "attack-options-line";

    /// <summary>
    /// Token shown when a disabled projection row omits <c>DisabledReason</c>.
    /// Matches <c>EngageAttackOrderResolver</c> so the menu shows the code the command façade returns.
    /// </summary>
    public const string MissingAbortToken = "BLOCKED";

    /// <remarks>
    /// Per-thread so concurrent callers never observe another thread's menu; Unity hosts bind on the main thread.
    /// </remarks>
    [ThreadStatic]
    private static BoundMenu? _lastBound;

    /// <summary>
    /// Binds preview text, button rows, and a replay-stable fingerprint from the engage-options menu.
    /// Returns the previously bound instance without allocating when the menu contents are unchanged,
    /// so per-frame host refreshes do not rebuild the preview.
    /// </summary>
    public static AttackOptionsPreviewState Bind(IReadOnlyList<EngageAttackOptions.AttackOption>? menu)
    {
        if (menu is null || menu.Count == 0)
        {
            return AttackOptionsPreviewState.Empty;
        }

        var cached = _lastBound;
        if (cached is not null && cached.Matches(menu))
        {
            return cached.State;
        }

        var state = Build(menu);
        _lastBound = new BoundMenu(Snapshot(menu), state);
        return state;
    }

    private static AttackOptionsPreviewState Build(IReadOnlyList<EngageAttackOptions.AttackOption> menu)
    {
        var rows = new AttackOptionMenuRow[menu.Count];
        var segments = new string[menu.Count];
        var fingerprintParts = new string[menu.Count];
        var anyBlocked = false;

        for (var i = 0; i < menu.Count; i++)
        {
            var option = menu[i];
            var abort = option.Enabled ? null : option.DisabledReason ?? MissingAbortToken;
            var buttonText = option.Enabled ? option.Label : $"{option.Label} ({abort})";
            var cue = option.Enabled ? AttackOptionsCueClasses.Ready : AttackOptionsCueClasses.Blocked;
            if (!option.Enabled)
            {
                anyBlocked = true;
            }

            rows[i] = new AttackOptionMenuRow(
                option.Id,
                AttackMenuPanelBinder.ResolveButtonName(option.Id),
                option.Label,
                option.Enabled,
                abort,
                buttonText,
                cue);
            segments[i] = buttonText;
            fingerprintParts[i] = string.Concat(
                option.Id,
                ":",
                option.Enabled ? "1" : "0",
                ":",
                abort ?? "-");
        }

        return new AttackOptionsPreviewState(
            "ATTACK: " + string.Join(" | ", segments),
            Array.AsReadOnly(rows),
            anyBlocked ? AttackOptionsCueClasses.Blocked : AttackOptionsCueClasses.Ready,
            string.Concat("atk:", string.Join(";", fingerprintParts)));
    }

    /// <summary>Finds the bound row for a host button id. Returns null when the projection omitted it.</summary>
    public static AttackOptionMenuRow? FindRow(AttackOptionsPreviewState state, string optionId)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var rows = state.Rows;
        for (var i = 0; i < rows.Count; i++)
        {
            if (string.Equals(rows[i].OptionId, optionId, StringComparison.Ordinal))
            {
                return rows[i];
            }
        }

        return null;
    }

    private static EngageAttackOptions.AttackOption[] Snapshot(IReadOnlyList<EngageAttackOptions.AttackOption> menu)
    {
        var copy = new EngageAttackOptions.AttackOption[menu.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = menu[i];
        }

        return copy;
    }

    private sealed class BoundMenu
    {
        private readonly EngageAttackOptions.AttackOption[] _options;

        public BoundMenu(EngageAttackOptions.AttackOption[] options, AttackOptionsPreviewState state)
        {
            _options = options;
            State = state;
        }

        public AttackOptionsPreviewState State { get; }

        public bool Matches(IReadOnlyList<EngageAttackOptions.AttackOption> menu)
        {
            if (menu.Count != _options.Length)
            {
                return false;
            }

            for (var i = 0; i < _options.Length; i++)
            {
                var option = menu[i];
                if (!ReferenceEquals(option, _options[i]) && !_options[i].Equals(option))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
