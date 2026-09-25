#!/usr/bin/env bash
# Source this file:  source infra/scripts/export-docker-env.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENV_FILE="${1:-$ROOT/.env}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo ".env not found at $ENV_FILE. Copy .env.example to .env." >&2
  return 1 2>/dev/null || exit 1
fi

while IFS= read -r line || [[ -n "$line" ]]; do
  line="${line#"${line%%[![:space:]]*}"}"
  [[ -z "$line" || "$line" == \#* ]] && continue
  key="${line%%=*}"
  value="${line#*=}"
  case "$key" in
    MSSQL_SA_PASSWORD|SQL_SERVER|SQL_DATABASE|SQL_USER|SQL_ENCRYPT|SQL_TRUST_SERVER_CERTIFICATE|SQL_MULTIPLE_ACTIVE_RESULT_SETS|ASPNETCORE_ENVIRONMENT|ASPNETCORE_FORWARDEDHEADERS_ENABLED)
      export "$key=$value"
      ;;
  esac
done < "$ENV_FILE"

export ASHA_SQL_SOURCE=docker
echo "Loaded SQL settings from $ENV_FILE"
echo "ASHA_SQL_SOURCE=docker — Google / Square / admin still come from secrets.json"
