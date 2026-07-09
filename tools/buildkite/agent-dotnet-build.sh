#!/usr/bin/env bash
# Bootstrap wrapper for the build-only Buildkite step (Task 3 split). Mirrors
# agent-dotnet-ci.sh's bootstrap-then-exec pattern.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# shellcheck source=agent-bootstrap-dotnet.sh
source "$repo_root/tools/buildkite/agent-bootstrap-dotnet.sh"
exec bash "$repo_root/tools/buildkite/dotnet-build.sh"
