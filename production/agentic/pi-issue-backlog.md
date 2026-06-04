# PI Issue Backlog (Agent F)

Generated from Phase 1 discovery. Copy into GitHub Issues as needed.

---

## PI-001 — JSON round-trip tests for scenario policy logistics

**Affected area:** Sim / scenario JSON  
**Symbols:** `ScenarioPolicyJsonDto`, `ScenarioPolicyJsonLoader`  
**Risk:** Low  
**Parallel:** Yes (Agent C)

**Acceptance criteria**

- [ ] Round-trip preserves `logTickBurn`, fuel fractions, comms block
- [ ] Unknown JSON properties do not break deserialize
- [ ] `dotnet test` Sim.Tests green

**Validation:** `dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj -v minimal`

---

## PI-002 — Catalog sensors JSON round-trip

**Affected area:** Data / catalog  
**Symbols:** `CatalogJsonImporter.ReadSensorBindings`  
**Risk:** Low  

**Acceptance criteria**

- [ ] Import sample JSON → bindings → re-serialize → same ordering/values

---

## PI-003 — SqliteCatalogReader pragma whitelist

**Affected area:** Data / catalog security  
**Symbols:** `SqliteCatalogReader.TableHasColumn`  
**Risk:** Low  

**Acceptance criteria**

- [ ] Only whitelisted table names used in pragma query
- [ ] Existing catalog tests pass

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

**Validation:** `production/qa/c2-manual-signoff-2026-06-02.md`