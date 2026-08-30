#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=sipacul-common.sh
. "$SCRIPT_DIR/sipacul-common.sh"

REPOSITORY_ROOT="$SIPACUL_DEFAULT_REPOSITORY_ROOT"
ENVIRONMENT_FILE="$SIPACUL_DEFAULT_ENVIRONMENT_FILE"
COMPOSE_PROJECT="$SIPACUL_DEFAULT_COMPOSE_PROJECT"
STATE_DIRECTORY="$SIPACUL_DEFAULT_STATE_DIRECTORY"
BACKUP_OUTPUT_DIRECTORY="$SIPACUL_DEFAULT_BACKUP_DIRECTORY"
HEALTH_TIMEOUT_SECONDS="$SIPACUL_DEFAULT_HEALTH_TIMEOUT_SECONDS"
EXECUTE=false
ACKNOWLEDGE_DATABASE_COMPATIBILITY=false
PENDING_FILE=""
STAGE="preflight"
TEMP_RELEASE_FILE=""

usage() {
    cat <<'EOF'
Usage:
  sudo ./operations/linux/application-rollback.sh [options]

Options:
  --repository-root PATH
  --environment-file PATH
  --compose-project NAME
  --state-directory PATH
  --backup-output-directory PATH
  --health-timeout-seconds N
  --execute
  --acknowledge-database-compatibility
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --repository-root) REPOSITORY_ROOT="$2"; shift 2 ;;
        --environment-file) ENVIRONMENT_FILE="$2"; shift 2 ;;
        --compose-project) COMPOSE_PROJECT="$2"; shift 2 ;;
        --state-directory) STATE_DIRECTORY="$2"; shift 2 ;;
        --backup-output-directory) BACKUP_OUTPUT_DIRECTORY="$2"; shift 2 ;;
        --health-timeout-seconds) HEALTH_TIMEOUT_SECONDS="$2"; shift 2 ;;
        --execute) EXECUTE=true; shift ;;
        --acknowledge-database-compatibility)
            ACKNOWLEDGE_DATABASE_COMPATIBILITY=true
            shift
            ;;
        -h|--help) usage; exit 0 ;;
        *) sipacul_die "Argument tidak dikenal: $1" ;;
    esac
done

on_exit() {
    local code=$?
    trap - EXIT
    set +e
    if [ -n "$PENDING_FILE" ] && [ -f "$PENDING_FILE" ]; then
        python3 - "$PENDING_FILE" "$STAGE" <<'PY'
import datetime
import json
import os
import sys

path, stage = sys.argv[1], sys.argv[2]
try:
    with open(path, "r", encoding="utf-8") as handle:
        value = json.load(handle)
    value["status"] = "failed"
    value["stage"] = stage
    value["failedAtUtc"] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(value, handle, indent=2)
        handle.write("\n")
    os.chmod(tmp, 0o600)
    os.replace(tmp, path)
except Exception:
    pass
PY
    fi
    [ -z "$TEMP_RELEASE_FILE" ] || rm -f "$TEMP_RELEASE_FILE"
    if [ "$code" -ne 0 ]; then
        printf '\n[GAGAL] Application rollback Linux berhenti pada stage %s.\n' "$STAGE" >&2
        printf '[AMAN] Rollback tidak pernah menjalankan migrator atau restore database.\n' >&2
        [ -z "$PENDING_FILE" ] || printf '[INFO] Pending operation: %s\n' "$PENDING_FILE" >&2
    fi
    exit "$code"
}
trap on_exit EXIT

sipacul_require_root
for command_name in docker git python3 realpath; do
    sipacul_require_command "$command_name"
done
case "$HEALTH_TIMEOUT_SECONDS" in
    ''|*[!0-9]*) sipacul_die "Health timeout harus integer positif." ;;
esac
[ "$HEALTH_TIMEOUT_SECONDS" -gt 0 ] || sipacul_die "Health timeout harus lebih dari 0."

sipacul_resolve_repository_root "$REPOSITORY_ROOT"
sipacul_assert_git_clean
ENVIRONMENT_FILE="$(sipacul_resolve_file "$ENVIRONMENT_FILE" "$REPOSITORY_ROOT" "Production environment")"
STATE_DIRECTORY="$(sipacul_resolve_directory "$STATE_DIRECTORY" "$REPOSITORY_ROOT")"
BACKUP_OUTPUT_DIRECTORY="$(sipacul_resolve_directory "$BACKUP_OUTPUT_DIRECTORY" "$REPOSITORY_ROOT")"
sipacul_assert_outside_repository "$STATE_DIRECTORY" "StateDirectory"
sipacul_assert_outside_repository "$BACKUP_OUTPUT_DIRECTORY" "BackupOutputDirectory"
sipacul_assert_production_environment "$ENVIRONMENT_FILE"

CURRENT_FILE="$STATE_DIRECTORY/current-deployment.json"
PENDING_FILE="$STATE_DIRECTORY/pending-operation.json"
CURRENT_RELEASE_FILE="$STATE_DIRECTORY/current-release.env"

[ ! -f "$PENDING_FILE" ] || \
    sipacul_die "Pending deployment operation ditemukan: $PENDING_FILE. Rollback ditolak sampai investigasi selesai."
[ -f "$CURRENT_FILE" ] || sipacul_die "Managed deployment state tidak ditemukan: $CURRENT_FILE"
[ -f "$CURRENT_RELEASE_FILE" ] || sipacul_die "current-release.env tidak ditemukan: $CURRENT_RELEASE_FILE"

sipacul_assert_state_schema "$CURRENT_FILE"
sipacul_assert_release_environment_matches_state "$CURRENT_RELEASE_FILE" "$CURRENT_FILE"

DATABASE_SHA="$(sipacul_json_get "$CURRENT_FILE" databaseReleaseSha)"
RUNTIME_SHA="$(sipacul_json_get "$CURRENT_FILE" runtimeReleaseSha)"
TARGET_SHA="$(sipacul_json_get "$CURRENT_FILE" previousRuntimeReleaseSha)"
REGISTRY_OWNER="$(sipacul_json_get "$CURRENT_FILE" registryOwner)"
STATE_PROJECT="$(sipacul_json_get "$CURRENT_FILE" composeProject)"

[ "$STATE_PROJECT" = "$COMPOSE_PROJECT" ] || sipacul_die "ComposeProject tidak cocok dengan deployment state."
[ -n "$TARGET_SHA" ] || sipacul_die "previousRuntimeReleaseSha tidak tersedia; application rollback tidak memiliki target."
TARGET_SHA="$(sipacul_normalize_sha "$TARGET_SHA")"
DATABASE_SHA="$(sipacul_normalize_sha "$DATABASE_SHA")"
RUNTIME_SHA="$(sipacul_normalize_sha "$RUNTIME_SHA")"
[ "$TARGET_SHA" != "$RUNTIME_SHA" ] || sipacul_die "Target rollback sama dengan runtime release aktif."
sipacul_validate_registry_owner "$REGISTRY_OWNER"

TEMP_RELEASE_FILE="$(mktemp /tmp/sipacul-rollback-plan.XXXXXX)"
sipacul_write_release_environment "$TEMP_RELEASE_FILE" "$DATABASE_SHA" "$TARGET_SHA" "$REGISTRY_OWNER"
sipacul_compose "$TEMP_RELEASE_FILE" config --quiet

printf '=== PREFLIGHT APPLICATION ROLLBACK SIPACUL (LINUX) ===\n'
sipacul_ok "Repository: $REPOSITORY_ROOT"
sipacul_ok "Database release tetap: $DATABASE_SHA"
sipacul_ok "Runtime release aktif: $RUNTIME_SHA"
sipacul_ok "Target runtime rollback: $TARGET_SHA"
sipacul_ok "Migrator tetap menunjuk database release $DATABASE_SHA dan tidak akan dijalankan."

printf '\n=== ROLLBACK PLAN ===\n'
printf '1. Pull dan verifikasi API/frontend/edge target rollback.\n'
printf '2. Pastikan PostgreSQL sehat dan buat backup sebelum switch runtime.\n'
printf '3. Hentikan edge/frontend/API.\n'
printf '4. Pertahankan migrator pada database release; hanya API/frontend/edge diganti.\n'
printf '5. Mulai API, frontend, edge dengan --no-deps lalu tunggu health check.\n'
printf '6. Simpan application-rollback state dan history.\n'
printf '[INFO] Rollback tidak menjalankan migrator dan tidak melakukan restore database.\n'

if [ "$EXECUTE" != "true" ]; then
    rm -f "$TEMP_RELEASE_FILE"
    TEMP_RELEASE_FILE=""
    PENDING_FILE=""
    printf '\n=== STATUS AKHIR PLAN ===\n'
    sipacul_ok "Plan-only selesai; runtime, database, image cache, release env, dan state tidak diubah."
    sipacul_ok "Execute membutuhkan --execute --acknowledge-database-compatibility."
    exit 0
fi

[ "$ACKNOWLEDGE_DATABASE_COMPATIBILITY" = "true" ] || \
    sipacul_die "Application rollback memerlukan --acknowledge-database-compatibility karena schema database tidak diturunkan."

rm -f "$TEMP_RELEASE_FILE"
TEMP_RELEASE_FILE=""

printf '\n=== PULL ROLLBACK RUNTIME IMAGES ===\n'
STAGE="pull-images"
for pair in \
    "API|$(sipacul_image_ref "$REGISTRY_OWNER" "api" "$TARGET_SHA")" \
    "Frontend|$(sipacul_image_ref "$REGISTRY_OWNER" "frontend" "$TARGET_SHA")" \
    "Edge|$(sipacul_image_ref "$REGISTRY_OWNER" "edge" "$TARGET_SHA")"
do
    label="${pair%%|*}"
    image="${pair#*|}"
    sipacul_info "Pull $label: $image"
    sipacul_assert_image_revision "$image" "$TARGET_SHA"
    sipacul_ok "$label image revision cocok."
done

printf '\n=== PRE-ROLLBACK BACKUP ===\n'
STAGE="pre-rollback-backup"
sipacul_compose "$CURRENT_RELEASE_FILE" up --detach --no-build postgres >/dev/null
sipacul_wait_service_healthy "$CURRENT_RELEASE_FILE" postgres "$HEALTH_TIMEOUT_SECONDS"

BEFORE_BACKUPS="$(find "$BACKUP_OUTPUT_DIRECTORY" -maxdepth 1 -type f -name 'sipacul-postgres-*.dump' 2>/dev/null | sort || true)"
"$SCRIPT_DIR/backup-postgres.sh" \
    --repository-root "$REPOSITORY_ROOT" \
    --environment-file "$ENVIRONMENT_FILE" \
    --compose-project "$COMPOSE_PROJECT" \
    --output-directory "$BACKUP_OUTPUT_DIRECTORY"
AFTER_BACKUPS="$(find "$BACKUP_OUTPUT_DIRECTORY" -maxdepth 1 -type f -name 'sipacul-postgres-*.dump' | sort)"
BACKUP_FILE="$(
    python3 - "$BEFORE_BACKUPS" "$AFTER_BACKUPS" <<'PY'
import sys
before = {x for x in sys.argv[1].splitlines() if x}
after = [x for x in sys.argv[2].splitlines() if x]
new = [x for x in after if x not in before]
if len(new) != 1:
    raise SystemExit(f"Backup pre-rollback harus menghasilkan tepat satu archive baru; aktual {len(new)}")
print(new[0])
PY
)"
[ -f "$BACKUP_FILE.sha256" ] && [ -f "$BACKUP_FILE.json" ] || \
    sipacul_die "Backup sidecar tidak lengkap: $BACKUP_FILE"
sipacul_ok "Backup pre-rollback: $BACKUP_FILE"

mkdir -p "$STATE_DIRECTORY/history"
chmod 750 "$STATE_DIRECTORY" "$STATE_DIRECTORY/history"

STAGE="prepared"
python3 - \
    "$PENDING_FILE" \
    "$DATABASE_SHA" \
    "$RUNTIME_SHA" \
    "$TARGET_SHA" \
    "$BACKUP_FILE" \
    "$COMPOSE_PROJECT" \
    "$REGISTRY_OWNER" <<'PY'
import datetime
import json
import os
import sys

path, database_sha, runtime_sha, target_sha, backup_file, project, owner = sys.argv[1:]
value = {
    "schemaVersion": 1,
    "application": "SiPacul",
    "operation": "application-rollback",
    "status": "in-progress",
    "stage": "prepared",
    "startedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "databaseReleaseSha": database_sha,
    "fromRuntimeReleaseSha": runtime_sha,
    "targetRuntimeReleaseSha": target_sha,
    "backupFile": backup_file,
    "composeProject": project,
    "registryOwner": owner,
}
tmp = path + ".tmp"
with open(tmp, "w", encoding="utf-8", newline="\n") as handle:
    json.dump(value, handle, indent=2)
    handle.write("\n")
os.chmod(tmp, 0o600)
os.replace(tmp, path)
PY

printf '\n=== SWITCH RUNTIME ONLY ===\n'
STAGE="stop-runtime"
sipacul_compose "$CURRENT_RELEASE_FILE" stop edge frontend api >/dev/null

# ROLLBACK_INVARIANT_NO_MIGRATOR_EXECUTION
sipacul_write_release_environment "$CURRENT_RELEASE_FILE" "$DATABASE_SHA" "$TARGET_SHA" "$REGISTRY_OWNER"
sipacul_compose "$CURRENT_RELEASE_FILE" config --quiet
sipacul_ok "Release environment rollback aktif; migrator tetap $DATABASE_SHA."

for service in api frontend edge; do
    STAGE="start-$service"
    sipacul_compose "$CURRENT_RELEASE_FILE" \
        up --detach --no-build --no-deps "$service" >/dev/null
    sipacul_wait_service_healthy "$CURRENT_RELEASE_FILE" "$service" "$HEALTH_TIMEOUT_SECONDS"
    sipacul_ok "$service sehat pada runtime rollback."
done

STAGE="finalize"
python3 - \
    "$CURRENT_FILE" \
    "$DATABASE_SHA" \
    "$TARGET_SHA" \
    "$RUNTIME_SHA" \
    "$REGISTRY_OWNER" \
    "$COMPOSE_PROJECT" \
    "$ENVIRONMENT_FILE" \
    "$CURRENT_RELEASE_FILE" \
    "$BACKUP_FILE" <<'PY'
import datetime
import json
import os
import sys

(
    path,
    database_sha,
    runtime_sha,
    previous_runtime_sha,
    owner,
    project,
    environment_file,
    release_environment_file,
    backup_file,
) = sys.argv[1:]
value = {
    "schemaVersion": 1,
    "application": "SiPacul",
    "status": "application-rollback",
    "databaseReleaseSha": database_sha,
    "runtimeReleaseSha": runtime_sha,
    "previousRuntimeReleaseSha": previous_runtime_sha,
    "registryOwner": owner,
    "composeProject": project,
    "environmentFile": environment_file,
    "releaseEnvironmentFile": release_environment_file,
    "backupFile": backup_file,
    "deployedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
}
tmp = path + ".tmp"
with open(tmp, "w", encoding="utf-8", newline="\n") as handle:
    json.dump(value, handle, indent=2)
    handle.write("\n")
os.chmod(tmp, 0o600)
os.replace(tmp, path)
PY

HISTORY_PATH="$(sipacul_write_history_copy "$STATE_DIRECTORY" "$CURRENT_FILE" application-rollback "$TARGET_SHA")"
rm -f "$PENDING_FILE"
PENDING_FILE=""

printf '\n=== STATUS AKHIR APPLICATION ROLLBACK ===\n'
sipacul_ok "Database release tetap: $DATABASE_SHA"
sipacul_ok "Runtime release sekarang: $TARGET_SHA"
sipacul_ok "Previous runtime sekarang: $RUNTIME_SHA"
sipacul_ok "Backup pre-rollback dipertahankan: $BACKUP_FILE"
sipacul_ok "Rollback history: $HISTORY_PATH"
sipacul_ok "Migrator tidak dijalankan; database tidak direstore atau didowngrade."
