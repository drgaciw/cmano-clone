#!/usr/bin/env bash
# Ensures a .NET SDK of at least 8.x is on PATH for CI agents (Buildkite + GitHub Actions).
# Hosted images may ship dotnet 6/7 on PATH; global.json targets net8.0.
#
# IMPORTANT — why this accepts SDK >= 8 rather than exactly 8.x:
# global.json pins 8.0.400 with "rollForward": "latestMajor", so on any agent that also
# has a newer SDK installed, `dotnet --version` resolves to that newer SDK (e.g. 10.0.302)
# even when 8.0.400 is present. A guard of `grep '^8\.'` therefore can NEVER pass on such
# an agent — it installs 8.0.400 successfully and then rejects its own install. That is the
# failure mode that broke CI once agent images picked up .NET 10:
#     ERROR: .NET 8 SDK required after bootstrap; got 10.0.302
# Building net8.0 with a newer SDK is explicitly sanctioned by the project:
# docs/engine-reference/dotnet/README.md — "Roll-forward: latestMajor (SDK 10.x may build locally)".
set -euo pipefail

# Quiet the .NET SDK's first-run welcome/telemetry banner for bare local/manual
# invocations of this script. NOTE: these do NOT fix the duplicated
# "=== dotnet SDK resolved ... ===" / `dotnet --info` output seen in CI build
# logs — that turned out to be this script being *sourced twice* in the same
# process tree (agent-dotnet-ci.sh sources it, then execs into dotnet-ci.sh,
# which sources it again), not a first-run banner. See the idempotency guard
# below, which is the actual fix. `.buildkite/pipeline.yml` already sets both
# of these at the pipeline `env:` level, so in CI they are redundant; they are
# kept here only so a bare local invocation gets the same quiet behavior
# without depending on pipeline env.
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# --- DOTNET_ROOT resolution --------------------------------------------------
# Opt-in SDK cache reuse across Buildkite jobs:
#   Hosted-agent cache volumes mount WORKSPACE-RELATIVE paths, so the default
#   $HOME/.dotnet can never be picked up by a pipeline `cache:` block — every
#   job re-downloads and re-extracts the ~212MB SDK tarball (~10s of every
#   job's runtime; measured at ~11h/month of billed agent time as of
#   Buildkite build #1703).
#
#   To opt in to a cached, reusable SDK, either:
#     (a) set `DOTNET_ROOT: .dotnet` (or another workspace-relative path) in
#         the pipeline step's `env:` and add a matching `cache:` block for
#         that path, so the extracted SDK persists across job runs on the
#         same agent; or
#     (b) bake the SDK into the Buildkite Agent Image (PREFERRED — needs no
#         pipeline YAML change at all, and the early-return below still
#         short-circuits the download/extract on every job).
#   The default remains $HOME/.dotnet, so behavior is unchanged until someone
#   explicitly opts in by setting DOTNET_ROOT.
DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
# A relative DOTNET_ROOT breaks `dotnet` subprocess resolution (dotnet
# re-derives framework/SDK search paths from its own absolute install
# location), so normalise to an absolute path before exporting it.
case "$DOTNET_ROOT" in
  /*) : ;; # already absolute
  *) DOTNET_ROOT="$(pwd)/${DOTNET_ROOT}" ;;
esac
export DOTNET_ROOT

dotnet_major_version() {
  if ! command -v dotnet >/dev/null 2>&1; then
    echo 0
    return
  fi
  dotnet --version 2>/dev/null | cut -d. -f1 || echo 0
}

ensure_dotnet_8() {
  local major
  major="$(dotnet_major_version)"

  # An SDK already resolving to 8 or newer satisfies global.json's latestMajor roll-forward.
  if [[ "$major" -ge 8 ]]; then
    return 0
  fi

  # A previously bootstrapped SDK under DOTNET_ROOT counts too. When DOTNET_ROOT
  # points at a cached, workspace-relative directory (see opt-in comment
  # above), this is the branch that turns a cached SDK into a cache HIT
  # instead of a re-download.
  if [[ -x "${DOTNET_ROOT}/dotnet" ]]; then
    export PATH="${DOTNET_ROOT}:${PATH}"
    if [[ "$(dotnet_major_version)" -ge 8 ]]; then
      echo "=== dotnet SDK reused from ${DOTNET_ROOT} (cache hit) ==="
      return 0
    fi
  fi

  echo "=== Installing .NET SDK 8.0.400 (agent dotnet missing or major=${major}) ==="
  mkdir -p "$DOTNET_ROOT"
  if ! curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.400 --install-dir "$DOTNET_ROOT"; then
    echo "ERROR: dotnet-install.sh failed"
    exit 1
  fi
  export PATH="${DOTNET_ROOT}:${PATH}"
}

# --- Idempotency guard -------------------------------------------------------
# This script is sourced TWICE in the same process tree on the normal CI path:
#   agent-dotnet-ci.sh:  source agent-bootstrap-dotnet.sh ; exec bash dotnet-ci.sh
#   dotnet-ci.sh:16          source agent-bootstrap-dotnet.sh   <-- again, post-exec
# (same shape on the replay path: agent-baltic-replay.sh sources this, then
# execs into baltic-replay.sh, which sources it again.)
#
# `exec` preserves the exported environment, so on the second source
# dotnet_major_version already resolves >= 8 and, without a guard, the whole
# tail below re-runs: the resolved-version echo AND a second full
# `dotnet --info` dump. That double dump — not a first-run telemetry banner —
# is the actual duplication measured in Buildkite build #1703's log (the
# "=== dotnet SDK resolved ... ===" line appears twice, immediately followed
# the second time by a second complete `.NET SDK:` block at log line 96).
#
# dotnet-ci.sh:14 documents WHY it re-sources at all: "Re-apply bootstrap PATH
# so test hosts that spawn dotnet/node subprocesses inherit SDK 8." So the
# second (and any subsequent) source must still re-export PATH — only the
# expensive version-probing/echo/`dotnet --info` tail is skipped.
# The guard requires BOTH markers. Checking only DOTNET_BOOTSTRAP_DONE would abort
# the job under `set -u` if the marker were ever inherited without its companion
# (a stale pipeline `env:` entry, or a partially-propagated environment): line 115
# would expand an unbound DOTNET_BOOTSTRAP_DIR. Requiring the directory too means
# a half-set environment degrades to a full, correct bootstrap instead of a hard failure.
if [[ -n "${DOTNET_BOOTSTRAP_DONE:-}" && -n "${DOTNET_BOOTSTRAP_DIR:-}" ]]; then
  export PATH="${DOTNET_BOOTSTRAP_DIR}:${PATH}"
  echo "=== dotnet bootstrap already applied (PATH re-exported) ==="
else
  ensure_dotnet_8

  resolved_major="$(dotnet_major_version)"
  if [[ "$resolved_major" -lt 8 ]]; then
    echo "ERROR: .NET SDK 8 or newer required after bootstrap; got $(dotnet --version 2>/dev/null || echo missing)"
    exit 1
  fi
  echo "=== dotnet SDK resolved: $(dotnet --version) (global.json pins 8.0.400, rollForward=latestMajor) ==="

  dotnet --info

  # Record the resolved SDK location, exported so it survives `exec` into a
  # new bash process. DOTNET_BOOTSTRAP_DONE is the marker the guard above
  # checks; DOTNET_BOOTSTRAP_DIR is what a second (or third, etc.) source
  # re-prepends onto PATH without re-probing or re-logging anything.
  export DOTNET_BOOTSTRAP_DIR="$(dirname "$(command -v dotnet)")"
  export DOTNET_BOOTSTRAP_DONE=1
fi
