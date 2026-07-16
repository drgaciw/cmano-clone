# Adversarial TDD Hardening — Wave 4 (2026-07-08)

**Branch:** `docs/req-corpus-maturity-w4` (with Wave 4 docs + these pins)  
**Worktree:** `.worktrees/req-corpus-w4`  
**Mode:** Parallel tracks A4-a / A4-b / A4-c → characterization pins  
**Scope:** **Tests only** — zero production `.cs`, zero goldens/hash, zero `DelegationBridge`

## Audit domains (mapped to Wave 4 gate)

| Track | Domain | Wave 4 claim hardened |
|-------|--------|------------------------|
| **A4-a** | Doc 11 mechanical | FR-09; correct doc 17 filename; schema Shipped not New; 21 req files |
| **A4-b** | Gate artifacts | Consistency **0 BLOCKER**; design-review sample; epic/story 005 Complete; tracker W0–W4 + 10b Phase N |
| **A4-c** | RTM + indexes | Current floors + W4 stamp; FR-19 platform section; 403 historical; master index ≥1232; GR program note |

## Pins implemented (green)

| Test | File |
|------|------|
| `doc_11_reverse_ref_FR_09` | Wave4Doc11MechanicalPinsTests |
| `doc_11_related_links_to_correct_replay_aar_filename` | Wave4Doc11MechanicalPinsTests |
| `doc_11_schema_fixtures_mapping_is_shipped_not_new` | Wave4Doc11MechanicalPinsTests |
| `requirements_corpus_has_exactly_21_md_files` | Wave4Doc11MechanicalPinsTests |
| `consistency_report_2026_07_08_verdict_is_zero_blocker` | Wave4CorpusGateArtifactsPinsTests |
| `design_review_wave4_memo_exists_and_covers_sample_docs` | Wave4CorpusGateArtifactsPinsTests |
| `epic_story_005_complete_and_waves_closed` | Wave4CorpusGateArtifactsPinsTests |
| `tracker_program_note_w0_w4_complete` | Wave4CorpusGateArtifactsPinsTests |
| `architecture_rtm_header_has_current_gates_and_w4_stamp` | Wave4RtmIndexHonestyPinsTests |
| `architecture_rtm_has_platform_editor_fr19_section` | Wave4RtmIndexHonestyPinsTests |
| `rtm_closeout_403_is_marked_historical_not_current_alone` | Wave4RtmIndexHonestyPinsTests |
| `root_master_index_uses_current_tracker_and_test_floor` | Wave4RtmIndexHonestyPinsTests |
| `gr_index_program_note_corpus_complete_editor_active` | Wave4RtmIndexHonestyPinsTests |

**Count:** 13 new Facts.

## Verify

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Wave4Doc11|FullyQualifiedName~Wave4CorpusGateArtifacts|FullyQualifiedName~Wave4RtmIndex" -v minimal
```

Expected: **13 passed**.

## Invariants

- No production `src/**` outside `*.Tests`
- No golden / production hash edits
- Pins depend on Wave 4 docs present in the same tree (commit W4 docs + tests together or after W4 docs land on main)

## Backlog (not this pass)

1. Automated broken-link crawl for all `requirements/*.md`  
2. Formal FR reverse-ref field on docs 02–06, 12, 20  
3. Commit/merge W4 docs + these pins on user instruction  

## Sign-off

- Parallel agents A4-a / A4-b / A4-c + orchestrator verify  
- Date: 2026-07-08  
