#!/usr/bin/env bash
set -euo pipefail

operation=""
bank_id=""
content=""
query=""
base_url="http://localhost:8888"
api_key="${HINDSIGHT_API_KEY:-}"

usage() {
  cat <<'EOF'
Usage:
  tools/hindsight/invoke-hindsight.sh --operation retain --bank-id dev-cmano-clone --content "Fixed tests"
  tools/hindsight/invoke-hindsight.sh --operation recall --bank-id dev-cmano-clone --query "Hindsight integration"
  tools/hindsight/invoke-hindsight.sh --operation reflect --bank-id dev-cmano-clone --query "Sprint 49"

Options:
  --operation retain|recall|reflect
  --bank-id BANK
  --content TEXT         Required for retain
  --query TEXT           Required for recall and reflect
  --base-url URL         Default: http://localhost:8888
  --api-key TOKEN        Defaults to HINDSIGHT_API_KEY when set
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --operation|-Operation)
      operation="${2:-}"
      shift 2
      ;;
    --bank-id|-BankId)
      bank_id="${2:-}"
      shift 2
      ;;
    --content|-Content)
      content="${2:-}"
      shift 2
      ;;
    --query|-Query)
      query="${2:-}"
      shift 2
      ;;
    --base-url|-BaseUrl)
      base_url="${2:-}"
      shift 2
      ;;
    --api-key|-ApiKey)
      api_key="${2:-}"
      shift 2
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [[ -z "$operation" || -z "$bank_id" ]]; then
  usage >&2
  exit 2
fi

case "$operation" in
  retain|recall|reflect) ;;
  *)
    echo "Invalid --operation: $operation" >&2
    exit 2
    ;;
esac

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Required command not found: $1" >&2
    exit 127
  fi
}

require_command curl
require_command jq

bank_escaped="$(jq -rn --arg v "$bank_id" '$v|@uri')"
base_url="${base_url%/}"

headers=(-H "Accept: application/json" -H "Content-Type: application/json")
if [[ -n "$api_key" ]]; then
  headers+=(-H "Authorization: Bearer $api_key")
fi

post_json() {
  local path="$1"
  local body="$2"
  curl -fsS -X POST "${headers[@]}" -d "$body" "$base_url$path"
}

case "$operation" in
  retain)
    if [[ -z "$content" ]]; then
      echo "retain requires --content" >&2
      exit 2
    fi
    body="$(jq -cn --arg content "$content" '{items:[{content:$content,context:"agentic-dev"}],async:true}')"
    post_json "/v1/default/banks/$bank_escaped/memories/retain" "$body" | jq .
    ;;
  recall)
    if [[ -z "$query" ]]; then
      echo "recall requires --query" >&2
      exit 2
    fi
    body="$(jq -cn --arg query "$query" '{query:$query,budget:"mid",maxTokens:4096}')"
    post_json "/v1/default/banks/$bank_escaped/memories/recall" "$body" \
      | jq -r 'if (.results // []) | length > 0 then .results[].text else . end'
    ;;
  reflect)
    if [[ -z "$query" ]]; then
      echo "reflect requires --query" >&2
      exit 2
    fi
    body="$(jq -cn --arg query "$query" '{query:$query,budget:"mid",includeFacts:true}')"
    post_json "/v1/default/banks/$bank_escaped/reflect" "$body" \
      | jq -r 'if .text then .text else . end'
    ;;
esac
