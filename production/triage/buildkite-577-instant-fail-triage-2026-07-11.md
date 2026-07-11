# Buildkite Build #576–#577 Instant Failure Triage
**Date:** 2026-07-11  
**Builds:** https://buildkite.com/drgaciw/cmano-clone/builds/576 (failed)  
**Builds:** https://buildkite.com/drgaciw/cmano-clone/builds/577 (failed)  
**PR:** #264 (branch `stack/post-editor/s93-asset-production`)  
**Status:** Both builds show **FAILURE** with **zero duration** — no `:hammer:` output logged, indicating pipeline upload or pre-checkout failure.

---

## Summary

Builds #576 and #577 on PR #264 fail **instantly** (no job execution, no agent output). This is a different failure mode than typical CI flakes:
- Expected: jobs run, fail with error logs
- Observed: build marked FAILURE in ~0 seconds with no job output
- Last known SUCCESS: Build #569 on 2026-07-09T20:55:52Z
- All builds #570+ show the same instant-fail pattern

---

## Evidence & Analysis

### ✓ Code & Pipeline Verified
1. **Pipeline YAML byte-identical to main**
   ```
   Local: 7bd0eaa45d9ab869afb8884890d3046a4506c1df91be8c47f3e2419be3853f42
   Main:  7bd0eaa45d9ab869afb8884890d3046a4506c1df91be8c47f3e2419be3853f42
   ```

2. **Pipeline YAML syntax valid** (Python YAML parser OK)

3. **Local CI fully green** (all verification gates pass on this machine)
   - `dotnet build`: 0 errors, 0 warnings ✓
   - `dotnet test` (full suite): 1599+ tests, 0 failures ✓
   - `ReplayGoldenSuiteTests`: 6/6 pass ✓
   - `PlayModeSmokeHarnessTests`: 20/20 pass ✓
   - Hash `17144800277401907079` present in goldens ✓

4. **No uncommitted pipeline changes**
   ```bash
   git status .buildkite/ tools/buildkite/ → working tree clean
   git diff origin/main..HEAD -- .buildkite/ → no changes
   ```

5. **No hooks or local tooling issues**
   - `.buildkite/hooks/` directory does not exist (post-checkout hook was removed per commit a86793c)
   - Agent bootstrap scripts are unchanged from main

### ✗ Root Cause Unknown (requires API/UI access)

**What would cause instant-fail with no duration:**
1. **Buildkite pipeline upload rejection** — e.g., org-level webhook misconfiguration, branch protection settings conflict
2. **Graphite CI optimizer issue** — plugin might be injected at org level despite being removed from our YAML
3. **GitHub webhook delivery failure** — Buildkite not receiving the push event
4. **Buildkite agent pool issue** — e.g., no agents available, queue stuck, org-level settings block PR builds
5. **Network/DNS issue** — agent can't reach GitHub or Buildkite API during checkout

**Cannot diagnose without:**
- `BUILDKITE_API_TOKEN` to query build logs, job status, and agent output
- Access to Buildkite UI → Pipeline Settings → Steps (to verify `.buildkite/pipeline.yml` is the source)
- Access to Buildkite UI → Environment (to check if `GRAPHITE_CI_OPTIMIZER_TOKEN` or other conflicting settings exist)
- Access to GitHub webhook logs (Settings → Webhooks)

---

## Historical Context

### Build Timeline
- **#535–#559**: PR #263 upload/runtime issues, bisected in docs (2026-07-09)
- **#569**: Last SUCCESS (2026-07-09T20:55:52Z)
- **#570+**: All FAILURE with instant fail pattern (no duration)
- **#571, #576, #577**: Empty commit retriggers, all still fail instantly

### Commits Involved
- `a86793c`: "fix(ci): revert post-checkout hook; re-source dotnet bootstrap in dotnet-ci" — removed `.buildkite/hooks/post-checkout`
- `cbe3f0d`: "fix(ci): harden Buildkite agent bootstraps for hosted agents"
- `4bd8942`: "fix(ci): embed CMO golden in test output; minimal pipeline"
- `92d65c1`: "fix(ci): golden CMO export fallback + drop Graphite pipeline plugin" — removed Graphite plugin from YAML
- `cc98474`: (on ci/buildkite-pipeline-optimization branch) "fix(ci): restore pipeline.yml byte-identical to origin/main"

### Known Issues (Fixed)
- PR #263 builds #535–#559 had upload/runtime rejects due to Buildkite cache syntax and Graphite optimizer interactions → **resolved per docs**
- Post-checkout hook (65e7c7c) attempted to work around optimizer by re-uploading pipeline → **removed per a86793c**

---

## Hypothesis: Buildkite Org-Level Configuration Issue

**Most likely:** Buildkite org/pipeline settings are in an inconsistent state:

1. **Pipeline UI still reads from `main`** instead of PR branch
   - Buildkite UI → Pipeline → Settings → Steps may show "Graphite optimizer" plugin or stale default-branch config
   - When a PR build triggers, Buildkite fetches `.buildkite/pipeline.yml` from **main** (not from PR branch)
   - If main's pipeline has been changed (e.g., Graphite plugin added via org settings), it overrides PR branch

2. **Graphite optimizer plugin active at org level**
   - Even though our YAML doesn't include a plugin, Buildkite's org-level settings might inject a "graphite-ci-buildkite-plugin" step
   - If the plugin is misconfigured or returns an empty pipeline, Buildkite marks the build FAILURE immediately

3. **`GRAPHITE_CI_OPTIMIZER_TOKEN` mismatch**
   - Buildkite env variable might be set to an invalid or expired token
   - Optimizer fails to authenticate, returns empty pipeline

---

## Recommended Actions

### Immediate (this environment)

1. **Verify buildkite-ci.md setup checklist** (in-repo human verification)
   - [ ] Navigate to Buildkite → Pipeline `cmano-clone` → Settings → Steps
   - [ ] Confirm **"Read pipeline configuration from the repository"** is selected
   - [ ] Confirm path is `.buildkite/pipeline.yml` (not `.buildkite/pipeline.main.yml` or similar)
   - [ ] Confirm no Graphite plugin or other steps are inserted above or below our steps

2. **Check Buildkite Pipeline Environment**
   - [ ] Buildkite → Pipeline → Settings → Environment
   - [ ] If `GRAPHITE_CI_OPTIMIZER_TOKEN` is set: test by **clearing it temporarily** and retriggering #578
   - [ ] If `GRAPHITE_CI_OPTIMIZER_TOKEN` is empty: leave it empty (correct state per .env.example)

3. **Verify GitHub webhook delivery**
   - [ ] GitHub → Settings → Webhooks
   - [ ] Confirm Buildkite webhook is active and recent deliveries show 200 OK
   - [ ] If 5xx errors, redeliver the last push to trigger a fresh #578 build

4. **Retrigger build #578 after above checks**
   - [ ] Use Buildkite UI → Rebuild button or `bk build rebuild 577 --pipeline cmano-clone`
   - [ ] Monitor for instant-fail vs. normal execution (should see `:hammer:` log within 10 seconds)

### If Immediate Actions Fail

5. **Contact Buildkite support** (if org-level infra issue)
   - Share build URLs #576/#577
   - Request: "Builds fail instantly with no duration. No agent output. Pipeline YAML is valid and works locally. Agent bootstrap logs missing. Suspect pipeline upload rejection or plugin misconfiguration."
   - Provide: `.buildkite/pipeline.yml` SHA and current GRAPHITE_CI_OPTIMIZER_TOKEN status

6. **Fallback: Migrate to GitHub Actions**
   - If Buildkite CI is no longer viable, migrate to GitHub Actions `.github/workflows/dotnet-ci.yml` (previous workflow exists)
   - Reference: `docs/engineering/buildkite-ci.md` § "What stays on GitHub Actions" and `docs/engineering/github-workflows.md`

---

## File Verification Checklist

| File | Status | SHA256 / Notes |
|------|--------|--------|
| `.buildkite/pipeline.yml` | ✓ Valid | `7bd0eaa45d9ab869afb8884890d3046a4506c1df91be8c47f3e2419be3853f42` (identical to main) |
| `.buildkite/hooks/` | ✓ None | Post-checkout hook removed (a86793c) |
| `tools/buildkite/agent-dotnet-ci.sh` | ✓ OK | Sources bootstrap correctly; mirrors main |
| `tools/buildkite/agent-bootstrap-dotnet.sh` | ✓ OK | Installs .NET 8.0.400 when needed |
| `tools/buildkite/dotnet-ci.sh` | ✓ OK | Runs all gates; no syntax errors |
| Local build output | ✓ All green | 1599+ tests, 0 failures; replay 6/6; smoke 20/20 |

---

## Next Steps (User Approval Required)

**Option A: Org-Level Fix (Recommended)**
1. Human to verify/fix Buildkite org settings per "Immediate Actions" above
2. Retrigger build #578
3. If green, stack/submit PR #264
4. If still failing, escalate to Buildkite support with URLs and chat context

**Option B: Local Verification + Stack**
1. Assume Buildkite infra issue (timeout for org-level fix)
2. Confirm local CI is all green (already done ✓)
3. Run `gt submit` to create/update PR #264 with approval override (requires human approval)
4. Monitor CI externally; if resolved, merge; if not, escalate to Buildkite

**Option C: Emergency Fallback**
1. If Buildkite downtime or misconfiguration is confirmed by human, migrate to GitHub Actions CI temporarily
2. Use `git revert` + `git push` to revert Buildkite changes and restore GitHub Actions
3. Continue sprint work on GitHub Actions until Buildkite is stable

---

## References

- Buildkite CI docs: `docs/engineering/buildkite-ci.md`
- Setup checklist (§1–5): one-time Buildkite setup steps
- Troubleshooting table (§7): "Build fails ~1m with no `:hammer:` log" → likely cause is Graphite optimizer or pipeline upload rejection
- AGENTS.md § "Buildkite CI agents": agent skills and MCP server info (not currently available in this environment)
- Previous bisect: `docs/ci: record #263 Buildkite upload/runtime bisect; #559 green` (834599d)

---

## Contact

For questions or escalation:
- **Buildkite operations**: `buildkite-operations-engineer` skill / agent
- **CI/CD lead**: `buildkite-ci-lead` agent  
- **Pipeline author**: `buildkite-pipelines-engineer` agent

---

## Update 2026-07-11T21:02Z — minimal pipeline also fails (#578)

Pushed an ultra-minimal `.buildkite/pipeline.yml` (single `Build and test` step, no
soft_fail siblings, no retry, no emoji labels). **Build #578 failed instantly** with
the same no-duration status as #576/#577.

**Conclusion:** failure is independent of pipeline YAML content (byte-identical to
main and a minimal subset both fail). Restore full main pipeline; unblock requires
Buildkite UI/API access (`BUILDKITE_API_TOKEN` or human UI checklist above).

