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

## PI-004 — STRIKE_UNREACHABLE_FUEL (GDD AC-4)

**Affected area:** Validation engine  
**Symbols:** `ValidationRules.StrikeReachabilityRule`  
**Risk:** Medium (behavior)  

**Problem:** GDD asks for fuel-specific code; engine has `STRIKE_UNREACHABLE` with combat radius + fuel fraction pad.

**Proposed:** Document alias or add `STRIKE_UNREACHABLE_FUEL` when excess is fuel-dominated (separate story).

**Dependencies:** Logistics GDD + platform fuel in catalog

---

## PI-005 — Replay golden SHA-256 lines

**Affected area:** Regression / replay  
**Risk:** Low  

**Acceptance criteria**

- [ ] Optional `FINGERPRINT_SHA256=` in golden files for comms/engage scenarios

**Dependencies:** PR #55 merged

---

## PI-006 — Unity manual C2 sign-off

**Affected area:** QA  
**Risk:** N/A (manual)  

**Validation:** `production/qa/c2-manual-signoff-2026-06-02.md`