#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# shellcheck source=agent-bootstrap-gitnexus.sh
source "$repo_root/tools/buildkite/agent-bootstrap-gitnexus.sh"
exec bash "$repo_root/tools/buildkite/gitnexus-wiki.sh"
