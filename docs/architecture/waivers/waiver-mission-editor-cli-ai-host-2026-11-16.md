# Waiver: MissionEditor.Cli → Ai.Host in-process ProjectReference

| Field | Value |
|-------|-------|
| **Status** | Temporary — active until exit ticket closes |
| **Date** | 2026-11-16 (CT) |
| **Owner go** | 7:40 PM CT Nov 16 · phrase **Accept jev's calls** |
| **ADR** | [ADR-030](../adr-030-ai-host-boundary.md) (status unchanged: **Accepted**) |
| **Audit** | [DRG-771](https://linear.app/drgamtd-workspace/issue/DRG-771) §8 item 1 (CMA-31) |
| **Exit ticket** | [DRG-772](https://linear.app/drgamtd-workspace/issue/DRG-772) |
| **Introduced by** | [#860](https://github.com/drgaciw/cmano-clone/pull/860) |
| **Base tip** | `1552311d` |

## What is waived

The `ProjectReference` from `ProjectAegis.MissionEditor.Cli` to `ProjectAegis.Ai.Host` (present on main after #860) is a **temporary waiver** of ADR-030's preferred boundary: thin CLI verbs that call a **separate** `Ai.Host` process, not an in-process AI host inside the authoring CLI.

ADR-030's rejected-alternatives table states that putting AI inside the Mission Editor CLI "couples provider SDKs and credentials to the authoring CLI" and prefers "thin CLI verbs calling a separate `Ai.Host`." This waiver records that the current in-process edge is accepted only until the exit work lands — **not** as an ADR-030 amendment.

## What is not waived

- ADR-030 remains **Accepted**. Do not flip its status or rewrite its Decision section under this waiver.
- No AI packages on Unity-bound / Plugins-copied projects.
- No live providers, credentials, or paid-provider CI paths under this waiver.
- [DRG-728](https://linear.app/drgamtd-workspace/issue/DRG-728) (AI-DEC-2: provider, terms, cost ceiling) stays **open**. This waiver does not close or satisfy DRG-728.

## Exit criteria (DRG-772)

1. Replace the in-process reference with a **thin CLI verb calling a separate Ai.Host process** (IPC / process boundary).
2. Complete that process split **before** any live-provider or credential wiring.
3. Mark this waiver **exited** with the implementing PR link when DRG-772 Done.

## Non-goals (now)

- No code change in the waiver PR (no csproj edit, no process-host scaffold).
- No `architecture.md` index edit from Lane D (self-contained waiver; cite from PR body + DRG-772).

## References

- ADR-030: `docs/architecture/adr-030-ai-host-boundary.md`
- Audit finding: DRG-771 §8 item 1
- Related open decision: DRG-728
- Exit implementation: DRG-772
