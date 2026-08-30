#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=sipacul-common.sh
. "$SCRIPT_DIR/sipacul-common.sh"

REPOSITORY_ROOT="$SIPACUL_DEFAULT_REPOSITORY_ROOT"
ENVIRONMENT_FILE="$SIPACUL_DEFAULT_ENVIRONMENT_FILE"
COMPOSE_PROJECT="$SIPACUL_DEFAULT_COMPOSE_PROJECT"
OUTPUT_DIRECTORY="$SIPACUL_DEFAULT_BACKUP_DIRECTORY"

usage() {
    cat <<'EOF'
Usage:
  sudo ./operations/linux/backup-postgres.sh [options]

Options:
  --repository-root PATH
  --environment-file PATH
  --compose-project NAME
  --output-directory PATH
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --repository-root) REPOSITORY_ROOT="$2"; shift 2 ;;
        --environment-file) ENVIRONMENT_FILE="$2"; shift 2 ;;
        --compose-project) COMPOSE_PROJECT="$2"; shift 2 ;;
        --output-directory) OUTPUT_DIRECTORY="$2"; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) sipacul_die "Argument tidak dikenal: $1" ;;
    esac
done

sipacul_require_root
for command_name in docker git python3 realpath sha256sum; do
    sipacul_require_command "$command_name"
done

sipacul_resolve_repository_root "$REPOSITORY_ROOT"
ENVIRONMENT_FILE="$(sipacul_resolve_file "$ENVIRONMENT_FILE" "$REPOSITORY_ROOT" "Production environment")"
OUTPUT_DIRECTORY="$(sipacul_resolve_directory "$OUTPUT_DIRECTORY" "$REPOSITORY_ROOT")"
sipacul_assert_outside_repository "$OUTPUT_DIRECTORY" "OutputDirectory"
sipacul_assert_production_environment "$ENVIRONMENT_FILE"
sipacul_assert_git_clean

mkdir -p "$OUTPUT_DIRECTORY"
chmod 750 "$OUTPUT_DIRECTORY"

compose_base() {
    docker compose \
        --project-directory "$REPOSITORY_ROOT" \
        --env-file "$ENVIRONMENT_FILE" \
        --file "$COMPOSE_FILE" \
        --project-name "$COMPOSE_PROJECT" \
        "$@"
}

wait_postgres() {
    local id="$1" started now state
    started="$(date +%s)"
    while :; do
        state="$(docker inspect \
            --format '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' \
            "$id")"
        case "$state" in
            running\|healthy) return 0 ;;
            exited\|*|dead\|*) sipacul_die "Container PostgreSQL berhenti: $state" ;;
            running\|unhealthy) sipacul_die "Container PostgreSQL berstatus unhealthy." ;;
        esac
        now="$(date +%s)"
        [ $((now - started)) -lt 60 ] || sipacul_die "Container PostgreSQL tidak healthy dalam 60 detik."
        sleep 1
    done
}

GIT_HEAD_BEFORE="$(sipacul_git rev-parse HEAD)"
GIT_STATUS_BEFORE="$(sipacul_git status --porcelain=v1 --untracked-files=all)"

mapfile -t POSTGRES_IDS < <(compose_base ps --all --quiet postgres | sed '/^$/d')
[ "${#POSTGRES_IDS[@]}" -eq 1 ] || \
    sipacul_die "Service postgres harus memiliki tepat satu container; aktual ${#POSTGRES_IDS[@]}."
POSTGRES_ID="${POSTGRES_IDS[0]}"
wait_postgres "$POSTGRES_ID"

DATABASE_NAME="$(docker exec "$POSTGRES_ID" printenv POSTGRES_DB)"
DATABASE_USER="$(docker exec "$POSTGRES_ID" printenv POSTGRES_USER)"
POSTGRES_IMAGE="$(docker inspect --format '{{.Config.Image}}' "$POSTGRES_ID")"
PG_DUMP_VERSION="$(docker exec "$POSTGRES_ID" pg_dump --version)"

MIGRATION_SQL='SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;'
LATEST_MIGRATION="$(
    printf '%s\n' "$MIGRATION_SQL" |
        docker exec -i "$POSTGRES_ID" \
            psql -X -v ON_ERROR_STOP=1 \
            -U "$DATABASE_USER" -d "$DATABASE_NAME" -tA
)"
LATEST_MIGRATION="$(printf '%s' "$LATEST_MIGRATION" | tr -d '\r\n')"
[ -n "$LATEST_MIGRATION" ] || sipacul_die "Migration terakhir tidak ditemukan."

TIMESTAMP="$(date -u +%Y%m%dT%H%M%S%3NZ)"
BASE_NAME="sipacul-postgres-$TIMESTAMP.dump"
FINAL_DUMP="$OUTPUT_DIRECTORY/$BASE_NAME"
FINAL_CHECKSUM="$FINAL_DUMP.sha256"
FINAL_MANIFEST="$FINAL_DUMP.json"

for target in "$FINAL_DUMP" "$FINAL_CHECKSUM" "$FINAL_MANIFEST"; do
    [ ! -e "$target" ] || sipacul_die "Target backup sudah ada: $target"
done

TOKEN="$(python3 - <<'PY'
import uuid
print(uuid.uuid4().hex)
PY
)"
PARTIAL_DUMP="$FINAL_DUMP.partial-$TOKEN"
PARTIAL_CHECKSUM="$FINAL_CHECKSUM.partial-$TOKEN"
PARTIAL_MANIFEST="$FINAL_MANIFEST.partial-$TOKEN"
CONTAINER_DUMP="/tmp/sipacul-backup-$TOKEN.dump"
COMPLETED=false

cleanup() {
    local code=$?
    trap - EXIT
    if [ -n "${POSTGRES_ID:-}" ] && [ -n "${CONTAINER_DUMP:-}" ]; then
        docker exec "$POSTGRES_ID" rm -f "$CONTAINER_DUMP" >/dev/null 2>&1 || true
    fi
    if [ "$COMPLETED" != "true" ]; then
        rm -f \
            "${PARTIAL_DUMP:-}" "${PARTIAL_CHECKSUM:-}" "${PARTIAL_MANIFEST:-}" \
            "${FINAL_DUMP:-}" "${FINAL_CHECKSUM:-}" "${FINAL_MANIFEST:-}"
    fi
    exit "$code"
}
trap cleanup EXIT

printf '=== PREFLIGHT BACKUP SIPACUL POSTGRESQL (LINUX) ===\n'
sipacul_ok "Repository: $REPOSITORY_ROOT"
sipacul_ok "PostgreSQL healthy; migration $LATEST_MIGRATION."
sipacul_ok "Output backup di luar repository: $OUTPUT_DIRECTORY"

printf '\n=== MEMBUAT BACKUP ===\n'
docker exec "$POSTGRES_ID" \
    pg_dump --format=custom --compress=9 \
    --no-owner --no-privileges \
    --username "$DATABASE_USER" \
    --dbname "$DATABASE_NAME" \
    --file "$CONTAINER_DUMP"

docker exec "$POSTGRES_ID" pg_restore --list "$CONTAINER_DUMP" >/dev/null
docker cp "$POSTGRES_ID:$CONTAINER_DUMP" "$PARTIAL_DUMP" >/dev/null

[ -s "$PARTIAL_DUMP" ] || sipacul_die "Archive backup kosong."
SIZE_BYTES="$(stat -c '%s' "$PARTIAL_DUMP")"
SHA256="$(sha256sum "$PARTIAL_DUMP" | awk '{print toupper($1)}')"
printf '%s  %s\n' "$SHA256" "$BASE_NAME" >"$PARTIAL_CHECKSUM"

python3 - \
    "$PARTIAL_MANIFEST" \
    "$DATABASE_NAME" \
    "$LATEST_MIGRATION" \
    "$POSTGRES_IMAGE" \
    "$PG_DUMP_VERSION" \
    "$BASE_NAME" \
    "$SIZE_BYTES" \
    "$SHA256" <<'PY'
import datetime
import json
import os
import sys

(
    path,
    database,
    latest_migration,
    postgres_image,
    pg_dump_version,
    backup_file,
    size_bytes,
    sha256,
) = sys.argv[1:]

value = {
    "schemaVersion": 1,
    "application": "SiPacul",
    "createdAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "database": database,
    "latestMigration": latest_migration,
    "postgresImage": postgres_image,
    "pgDumpVersion": pg_dump_version,
    "backupFile": backup_file,
    "sizeBytes": int(size_bytes),
    "sha256": sha256,
}
with open(path, "x", encoding="utf-8", newline="\n") as handle:
    json.dump(value, handle, indent=2)
    handle.write("\n")
os.chmod(path, 0o600)
PY

chmod 600 "$PARTIAL_DUMP" "$PARTIAL_CHECKSUM"

mv "$PARTIAL_CHECKSUM" "$FINAL_CHECKSUM"
mv "$PARTIAL_MANIFEST" "$FINAL_MANIFEST"
mv "$PARTIAL_DUMP" "$FINAL_DUMP"

FINAL_SHA="$(sha256sum "$FINAL_DUMP" | awk '{print toupper($1)}')"
[ "$FINAL_SHA" = "$SHA256" ] || sipacul_die "Hash archive berubah setelah finalisasi."

GIT_HEAD_AFTER="$(sipacul_git rev-parse HEAD)"
GIT_STATUS_AFTER="$(sipacul_git status --porcelain=v1 --untracked-files=all)"
[ "$GIT_HEAD_AFTER" = "$GIT_HEAD_BEFORE" ] || sipacul_die "Git HEAD berubah selama backup."
[ "$GIT_STATUS_AFTER" = "$GIT_STATUS_BEFORE" ] || sipacul_die "Working tree berubah selama backup."

COMPLETED=true
docker exec "$POSTGRES_ID" rm -f "$CONTAINER_DUMP" >/dev/null
CONTAINER_DUMP=""

printf '\n=== STATUS AKHIR BACKUP ===\n'
sipacul_ok "Archive custom tervalidasi oleh pg_restore --list."
sipacul_ok "SHA256: $SHA256"
sipacul_ok "Manifest: $FINAL_MANIFEST"
sipacul_ok "Backup selesai: $FINAL_DUMP"
sipacul_ok "HEAD dan working tree tidak berubah; file sementara container sudah dihapus."
