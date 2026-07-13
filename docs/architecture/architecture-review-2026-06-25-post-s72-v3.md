# Architecture Review Report — Post S72 + Baltic v3 Foundations

**Date:** 2026-06-25  
**Mode:** `/architecture-review full` (post–S72 E7 prep + S73–S75 Baltic v3 content wave)  
**Engine:** Unity 6.3 LTS (6000.3.14f1) + .NET 8 headless  
**Authority:** ADRs 001–011 + Spirit1 hub ADR; GDD corpus `design/gdd/`; production boundaries S69–S75  
**Verdict:** **CONCERNS**

---

## Load summary

| Artifact | Count / state |
|----------|----------------|
| System GDDs | 11 (+ systems-index) |
| ADRs | 11 numbered (`adr-001` … `adr-011`) + Spirit1 frozen hub |
| Post-S72 surfaces | DOTS/MASS (S53), orbital DEW/Kessler (S54), Buildkite CI (S67), Baltic v2/v3 replay harness |
| GitNexus index | 20496 nodes / 38203 edges @ `b2c9411` |
| Test baseline | 1232/0f; ReplayGolden 6/6; C2 18/18; hash `17144800277401907079` (v2) |

---

## GitNexus watchlist (CRITICAL — no hotpath edits without impact ack)

| Symbol | Upstream impact | Risk | Phase C posture |
|--------|-----------------|------|-----------------|
| `CatalogWriteGate` | 178 | CRITICAL | Extend-only; v3 catalog JSON additive |
| `PatrolCandidateEngagePolicy` | 97 | CRITICAL | Read/verify only; no policy edits in S73–S75 |
| `DelegationBridge` | 127 | CRITICAL (exact) | **ZERO** `.cs` edits maintained |
| `BalticReplayHarness` | 52 | CRITICAL (exact) | v3 goldens isolated; v2 hash preserved |

All four match boundary §5 exact counts. S73–S75 stacks are content/docs-only relative to these symbols.

---

## Surface reviews

### DOTS spawn / MASS tier (S53, ADR-005)

**ADR-005** accepts DOTS/ECS for world state; delegation/policy remain plain C#. S53 delivered spawn skeleton + MASS tier benchmark prep (`stack/sprint52/`, S53 closeouts). **Status:** Partial — headless harness and pure-C# sim path remain merge authority; full ECS tick integration is pre-production stretch, not blocking Baltic v2/v3 content.

**Gap:** Production tick still collapses steps 6–7 in Delegation per `architecture.md`; ECS bridge snapshot builders not yet default path.

### Orbital DEW + Kessler (S54)

S54 closeouts document orbital verification fixtures and escalation paths under speculative/near-future systems (GDD #15, #19). **Status:** Partial — engineering spikes and verif logs exist; not wired into Baltic vertical slice or v3 manifest promotion path.

**Gap:** No Accepted ADR for orbital engagement domain validators beyond ADR-009 stubs; remains Full Vision / vertical-slice optional.

### Release-train / Buildkite ops (S67)

S67 COMPLETE: `.buildkite/preflight-s67.yml`, regression baseline lock, branch-protection docs. Buildkite skills vendored; MCP configured. **Status:** Covered for internal engineering train; commercial CI promotion still gated on Launch stage decision.

**Gap:** Store/certification pipelines not in scope until commercial launch execution gate (see `production/gate-checks/commercial-launch-execution-gate-TBD.md`).

### Baltic v2 / v3 replay harness

| Track | Policies | Goldens | Hash |
|-------|----------|---------|------|
| v2 (production) | 10 `baltic-v2-*` | 9 replay files | `17144800277401907079` locked |
| v3 (isolated) | 6 `baltic-v3-*` (S74) | 5 replay files (S74) + theater family (S75 wt) | Separate fingerprints until S80 ADR |

**Status:** Covered — `BalticReplayHarness` seam proven; v3 content additive per `production/baltic-v3-scope-boundary-2026-06-25.md`. ReplayGolden suite remains 6/6 (v2 authority).

**Gap:** v3 goldens not yet promoted to production hash; requires S80 content-complete gate + explicit ADR if hash changes.

---

## Traceability summary (vs GDDs + ADRs 001–011)

| Layer | Representative TRs | ADR coverage | Status |
|-------|-------------------|--------------|--------|
| Sim core / determinism | TR from sim-core, order-log GDDs | ADR-001, ADR-004 | ✅ Covered (headless proven) |
| Policy / engage | policy-roe, engagement GDDs | ADR-002, ADR-009 | ⚠️ Partial (engage unification pending) |
| Data / catalog | platform DB, editor GDDs | ADR-006, ADR-008, ADR-011 | ✅ Covered |
| C2 / presentation | command-and-control GDD | ADR-007, ADR-010 | ⚠️ Partial (headless-first; Editor evidence deferred) |
| DOTS / scale | simulation-core perf | ADR-005 | ⚠️ Partial (S53 skeleton; not default tick) |
| Agentic / AAR | agentic-infrastructure GDD | ADR-003, partial ADR-010 | ⚠️ Partial |
| Speculative / orbital | near-future GDD | — | ❌ Gap (S54 spikes only) |
| Baltic content v3 | design spec §3/§5 | Boundary doc (S73) | ✅ Covered (content scope) |
| Commercial launch | E7 prep boundary | Checklist v3, i18n spec | ⚠️ Partial (prep complete; execution gated) |

**Totals (qualitative refresh):** majority MVP spine ✅/⚠️; speculative/orbital and asset pipeline remain primary gaps.

---

## Cross-ADR consistency

| Check | Result |
|-------|--------|
| ADR-005 vs ADR-001 sim boundary | **OK** — pure C# rules first; DOTS for world state only |
| ADR-010 vs DelegationBridge ZERO | **OK** — adapter seam unchanged through S75 |
| ADR-006 CatalogWriteGate vs v3 JSON drops | **OK** — extend-only; no gate bypass |
| ADR-008 validation vs v3 policies | **OK** — new policies validate through existing engine |
| Buildkite (S67) vs headless gates | **OK** — preflight mirrors dotnet test + replay filters |
| v2 hash vs v3 isolated goldens | **OK** — no ADR conflict; promotion deferred S80 |

No new blocking cross-ADR conflicts detected.

---

## Engine compatibility

- Unity **6.3 LTS** + .NET **8.0.400** consistent across ADRs and `global.json`.
- Headless PlayMode smoke (18/18) remains CI authority without Editor.
- No deprecated API references flagged in ADR corpus review.

---

## Actionable gaps (priority order)

1. **Asset pipeline** — `design/assets/asset-manifest.md` missing; blocks Launch-stage store evidence (>5% pipeline threshold in commercial gate stub).
2. **DOTS production integration** — promote S53 spawn/MASS from skeleton to documented default path or ADR amendment for hybrid-only MVP.
3. **Orbital / speculative ADR** — file stub ADR or defer explicitly in systems-index if Full Vision remains post–Baltic v3.
4. **v3 golden promotion** — S80 gate + ADR before merging v3 hashes into production replay authority.
5. **Commercial launch execution** — stage advance to Launch + store accounts + locale production (explicitly out of S73–S75 scope).

---

## Verdict rationale

**CONCERNS** — The post–S72 architecture is **internally consistent** and **determinism-preserving** for the shippable Baltic v2 vertical slice. S73–S75 v3 content lands on documented seams without CRITICAL symbol edits. Remaining gaps are **expected program debt**: asset pipeline, full DOTS tick adoption, speculative systems, and commercial execution — not structural failures.

**PASS path:** asset-manifest Phase B complete + S80 v3 content gate + Launch stage decision with commercial execution gate checklist started.

**FAIL triggers (none active):** CRITICAL hotpath edits, hash regression without ADR, or cross-ADR dependency cycles.

---

## References

- `docs/architecture/architecture.md` (master blueprint — status line updated 2026-06-25)
- `docs/architecture/architecture-review-2026-06-03-full.md` (prior CONCERNS baseline)
- `production/baltic-v3-scope-boundary-2026-06-25.md`
- `production/commercial-launch-scope-boundary-2026-06-25.md`
- Graphite stack PRs #232–#235 (S73–S75 Phase C integration)
