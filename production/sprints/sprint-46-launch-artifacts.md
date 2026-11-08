# Sprint 46 — Launch Artifacts (B5)

**Dates:** ~TBD (~8–10 days)  
**Trunk:** `main` @ (post-S45)  
**Predecessor:** Sprint 45 — COMPLETE; **B1+B2 complete required**  
**Stage:** Release Enablement (Track B)  
**Authority:** B5 epic — [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §5, §9.

> **OUT-OF-BOUNDARY — PLANNING ONLY.**

## Sprint Goal

Produce launch artifacts: release checklist, store page drafts, localization pipeline spec, launch docs + evidence index. No code-heavy features — documentation and process deliverables primary.

## Capacity

- Total days: 10
- Buffer: 2 days
- **Effective dev-days**: **8**
- **Parallelism**: **5 tracks** (cloud-heavy)

## Tasks

### Must Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S46-01 | **Re-baseline + QA plan** | c-sharp-devops-engineer + team-qa | 1 | S45-06 | CI still green |
| S46-02 | **Release checklist** | release-manager | 2 | S46-01 | `production/release/` checklist complete |
| S46-03 | **Store pages draft** | community-manager + team-ui | 2 | S46-01 | Store copy + asset list |
| S46-04 | **Localization pipeline** | localization-lead | 2.5 | S46-01 | i18n pipeline spec + string extraction plan |
| S46-05 | **Launch docs + evidence index** | technical-writer | 1.5 | S46-01 | Consolidated launch doc pack |
| S46-06 | **Closeout** | coordinator | 0.5 | S46-02+ | B5 exit criteria met |

## Definition of Done

- [ ] Release checklist publishable
- [ ] Store page drafts ready for review
- [ ] Localization pipeline spec approved by localization-lead
- [ ] Launch evidence index links S35–S45 corpus
- [ ] B1+B2 prerequisites verified

## Parallel Execution Model

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| Release checklist | `stack/sprint46/release-checklist` | Cloud | S46-02 |
| Store pages | `stack/sprint46/store-pages` | Cloud | S46-03 |
| i18n pipeline | `stack/sprint46/i18n-pipeline` | Cloud | S46-04 |
| Launch docs | `stack/sprint46/launch-docs` | Cloud | S46-05 |
| Closeout | `stack/sprint46/closeout` | Local | S46-06 |

---

*Planning only. Requires B1+B2 complete.*
