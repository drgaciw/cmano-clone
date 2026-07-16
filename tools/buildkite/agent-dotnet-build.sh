#!/usr/bin/env bash
# GROUNDWORK ONLY — not called by live .buildkite/pipeline.yml.
# Bootstrap wrapper for future build-only step; mirrors agent-dotnet-ci.sh pattern.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# shellcheck source=agent-bootstrap-dotnet.sh
source "$repo_root/tools/buildkite/agent-bootstrap-dotnet.sh"
exec bash "$repo_root/tools/buildkite/dotnet-build.sh"
