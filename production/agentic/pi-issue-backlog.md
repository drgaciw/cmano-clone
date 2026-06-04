# PI Issue Backlog (Agent F)

Generated from Phase 1 discovery. Copy into GitHub Issues as needed.

---

## PI-001 — JSON round-trip tests for scenario policy logistics — **Done**

**Artifacts:** `ScenarioPolicyJsonRoundTripTests` (Sim.Tests)

---

## PI-002 — Catalog sensors JSON round-trip — **Done**

**Artifacts:** `CatalogJsonRoundTripTests` (Data.Tests)

---

## PI-003 — SqliteCatalogReader pragma whitelist — **Done**

**Artifacts:** `SqliteCatalogReader` table name whitelist (SEC-01)

---

## PI-004 — STRIKE_UNREACHABLE_FUEL (GDD AC-4) — **Done**

**Affected area:** Validation engine  
**Symbols:** `ReachabilityCalculator.TryClassifyStrikeUnreachable`, `ValidationRules.StrikeReachabilityRule`  

**Behavior:** Beyond combat radius → `STRIKE_UNREACHABLE`; inside radius but over fuel budget → `STRIKE_UNREACHABLE_FUEL`.

---

## PI-005 — Replay golden SHA-256 lines — **Done**

**Affected area:** Regression / replay  

- `FINGERPRINT_SHA256=` in `replay-golden-baltic-comms-2026-06-02.txt` and `replay-golden-baltic-engage-2026-06-02.txt`
- `ReplayGoldenAssertions.AssertPinnedHashes` (optional SHA line)

---

## PI-006 — Unity manual C2 sign-off

**Affected area:** QA  
**Risk:** N/A (manual)  

| Scope | Status |
|-------|--------|
| Headless proxy (agentic) | **Done** — `production/qa/pi-006-headless-proxy-2026-06-04.md` |
| Unity Editor manual | **Pending** — `production/qa/c2-manual-signoff-2026-06-02.md` |