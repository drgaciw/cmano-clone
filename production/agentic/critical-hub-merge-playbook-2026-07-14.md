# CRITICAL Hub Merge Playbook — Post-S93 Gate

**Date:** 2026-07-14  
**Authority:** [`post-s93-project-release-hold-gate-2026-07-14.md`](../gate-checks/post-s93-project-release-hold-gate-2026-07-14.md)  
**Stage:** Release (do not advance Launch via this playbook)

## Purpose

Any change that may touch high-blast-radius symbols must run GitNexus **before** commit and paste impact evidence into the PR/commit body. This playbook operationalizes gate §1 / §7.5.

## CRITICAL watchlist (upstream impact @ gate)

| Symbol | Risk | Impacted (gate) | Rule |
|--------|------|-----------------|------|
| `ScenarioDocumentEditor` | CRITICAL | 233 | Prefer CLI/authoring seams; impact before edit |
| `CatalogWriteGate` | CRITICAL | 186 | **extend-only** — no write-path rewrite |
| `DelegationBridge` | CRITICAL | 145 | **ZERO hotpath** — refuse without golden ADR |
| `PatrolCandidateEngagePolicy` | CRITICAL | 113 | Policy seam — impact + suite |
| `BalticReplayHarness` | CRITICAL | 54 | Replay/gauntlet — impact + ReplayGolden |

Related post-gauntlet surface: `GauntletOracleEvaluator` / `GauntletOracleExpect` — treat as **HIGH** for land/merge.

## Mandatory pre-edit / pre-land checklist

```bash
# 1) From the absolute checkout path you will modify:
cd /path/to/checkout
node .gitnexus/run.cjs status
# Must be up-to-date @ HEAD (run analyze if stale)

# 2) MCP or CLI impact (use absolute repo path if dual cmano-clone indexes exist):
# impact <Symbol> direction=upstream summaryOnly repo=/absolute/path

# 3) Detect change surface:
# detect_changes scope=all  OR  scope=compare base_ref=main

# 4) Floors after code change:
dotnet build ProjectAegis.sln -c Release --nologo   # 0e/0w
dotnet test ProjectAegis.sln -c Release --nologo    # failed=0; passed ≥ prior floor
```

## Hard refuse rules

1. **Do not edit** `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` hotpath without a golden ADR explicitly authorizing it.
2. **Do not rewrite** `CatalogWriteGate` write/commit paths — extend-only consumers and tests.
3. **Do not** change Baltic production hash `17144800277401907079` without golden ADR + regression lock process.
4. **Do not** set `production/stage.txt` to Launch without human Launch ack + commercial gate.

## Dual-index warning

Two GitNexus registrations may both be named `cmano-clone`:

- `/home/username01/cmano-clone`
- `/home/username01/projects/active/cmano-clone/cmano-clone`

Always pass the **absolute path** as `repo` for MCP tools.

## PR / commit body template

```
GitNexus:
- status: up-to-date @ <sha>
- detect_changes: risk=<low|med|high>; changed=<n>
- impact BalticReplayHarness upstream: CRITICAL <count> (expected)
- impact DelegationBridge: ZERO file edits in this change
- suite: <passed>/<failed>
Gate cite: post-s93-project-release-hold-gate-2026-07-14.md
```

---

*Cite this playbook on all Track A–C merges from the post-S93 remediation plan.*
