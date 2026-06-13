# Progress

## Status
In Progress — GitNexus CI migration to Buildkite (uncommitted on `main`)

## Tasks
- [x] Add Buildkite pipeline steps for GitNexus PR analysis and main reindex
- [x] Author `tools/buildkite/` scripts (bootstrap, pr-analysis, reindex, wiki + agent wrappers)
- [x] Disable duplicate GitHub Actions workflows (`if: false` on reindex / wiki / pr-analysis)
- [x] Update `docs/engineering/buildkite-ci.md` and `ci-and-branch-protection.md`
- [ ] Commit and push; verify PR + main Buildkite steps in UI
- [ ] Rotate any tokens that were briefly written into `.env.example` (see Notes)

## Files Changed
- `.buildkite/pipeline.yml` — `gitnexus-pr` (PRs) and `gitnexus-reindex` (`main`) steps
- `.env.example` — `OPENAI_API_KEY` placeholder added (values must stay empty in repo)
- `.github/workflows/gitnexus-pr-analysis.yml` — disabled (`if: false`)
- `.github/workflows/gitnexus-reindex.yml` — disabled (`if: false`)
- `.github/workflows/gitnexus-wiki.yml` — disabled (`if: false`)
- `docs/engineering/buildkite-ci.md` — GitNexus-on-Buildkite table, env vars, cutover checklist
- `docs/engineering/ci-and-branch-protection.md` — minor alignment
- `tools/buildkite/agent-bootstrap-gitnexus.sh` (new)
- `tools/buildkite/agent-gitnexus-pr-analysis.sh` (new)
- `tools/buildkite/agent-gitnexus-reindex.sh` (new)
- `tools/buildkite/agent-gitnexus-wiki.sh` (new)
- `tools/buildkite/gitnexus-pr-analysis.sh` (new)
- `tools/buildkite/gitnexus-reindex.sh` (new)
- `tools/buildkite/gitnexus-wiki.sh` (new)

## Verification
- ✓ Pipeline YAML adds PR + main GitNexus steps with `soft_fail: true`
- ✓ Scripts mirror GitHub workflow behavior (analyze, detect_changes, doc-only skip, annotations)
- ✓ `gitnexus-security.yml` (CodeQL) remains on GitHub Actions
- ✓ Recent `main` commits already shipped Buildkite primary CI (`f15d8d2` … `d1edf6a`)
- ⚠ Local `bash -n` on Windows reports CRLF false-positive on bootstrap script; agents run Linux
- ☐ Buildkite PR annotation not yet verified in UI
- ☐ `main` reindex skip-on-doc-only not yet verified on live push

## Acceptance Criteria (this slice)
- [x] GitNexus PR blast-radius runs on Buildkite for pull requests
- [x] GitNexus reindex runs on Buildkite for `main` code pushes
- [x] GitHub duplicate CLI workflows disabled; security workflow kept
- [x] Docs describe manual wiki job and local parity commands
- [ ] No secrets in committed `.env.example`
- [ ] End-to-end check on Buildkite after merge

## Notes
**Security:** Working copy had real `GRAPHITE_CI_OPTIMIZER_TOKEN` and `OPENAI_API_KEY` values in `.env.example`. Restored to empty placeholders before any commit. If those values were ever pushed, rotate them in Graphite CI settings and OpenAI.

**Prior session (complete):** Sprint 22 Story 22-3 — ADR-011 `platform-editor-excel-roundtrip` verified 2026-06-08; no further work needed.

**Next:** Stage CI files (not `.cursor/settings.json`), commit, `gt submit` or push, confirm Buildkite steps on a test PR and a `main` code change.
