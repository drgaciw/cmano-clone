# Platform Notes (Internal) — Store / Steam Submission Prep

**Date:** 2026-06-25  
**Track:** S70 internal notes (store pages companion)  
**Authority / MANDATORY CITES:** production/commercial-launch-scope-boundary-2026-06-25.md (prep only; submission OUT); future-sprint-roadpmap-062526.md §3/§6/§7/§10 ; roadmap-execute-plan-062526.md §3/§4 (S70 store S70-01/02); AGENTS.md ; S69 COMPLETE (smoke-sprint-69-closeout-2026-06-25.md gates PASS + GitNexus 19962/37627/2462 low + S69 artifacts); S66 release-checklist-v2.md + Baltic v2 manifest (input); AGENTS.md verification-before + GitNexus discipline.  

**GitNexus pre + verif-before:** Executed (list 19962/37627/2462; detect 24/0 low; impacts CRITICAL 178/97/127/52 exact); gates build 0e/1232/0f/6/6/18/18/hash/ZERO RUN+READ full (/tmp/gates-s70/ logs). Docs-only. Stage Release.  

**Scope note:** Internal checklist / notes for future submission. **NOT executed** in S69–S72 (E7 prep). All prep docs only.  

---

## Steam Submission Checklist (Internal Reference)

- App ID / package registration (future)  
- Store page fields populated from store-page-draft.md (short/long desc, features, tags)  
- Assets per asset-checklist.md (capsule 616x353, screenshots 5+, trailer)  
- Age rating / content descriptors (military sim; no violence glorification)  
- System reqs (headless .NET 8 core + Unity 6.3; Windows/Linux)  
- Pricing / regions (prep only)  
- Release date / visibility (TBD post S72)  
- Build depot upload (Baltic vertical slice + replay goldens; deterministic)  
- Controller support / accessibility (see design/accessibility-requirements.md)  
- Community features (Steam discussions, guides)  
- Localization (P0 en-US only per S71 plan; inventory)  
- Marketing / capsule review (post S70)  

## Platform-Specific Notes

**Steam:**  
- Use Steamworks SDK for future (not S70).  
- Tags emphasis: Simulation, Strategy, Indie, Military.  
- Replay / determinism as key differentiator (cite 6/6 golden + hash invariant).  
- Link to evidence-index (S66 v2 + S70 prep) in "About" or news.  

**Other (future):**  
- Epic / GOG parity notes deferred.  
- Console (no current plan).  

## Risks / Reminders (from boundary + AGENTS)

- ZERO DelegationBridge edits ever.  
- CatalogWriteGate extend-only (no writes in prep).  
- Hash immutable unless ADR.  
- No stage advance at S72.  
- Cite full authorities on any future submission artifacts.  
- GitNexus pre + full gates before any asset/code touch.  

**S70 platform-notes partial (internal list only).** Supersedes nothing; extends S46 template. Low risk docs.  

Cites enforced: commercial-launch-scope-boundary-2026-06-25.md + future-sprint-roadpmap-062526.md §3/§6/§7/§10 + roadmap-execute-plan-062526.md §3/§4 + AGENTS.md + S69 artifacts + S66 release-checklist-v2 + Baltic v2 corpus. Pre state + GitNexus pre + verif-before complete.  

*Independent S70 cloud track.*
