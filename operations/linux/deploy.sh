#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=sipacul-common.sh
. "$SCRIPT_DIR/sipacul-common.sh"

RELEASE_SHA=""
REPOSITORY_ROOT="$SIPACUL_DEFAULT_REPOSITORY_ROOT"
ENVIRONMENT_FILE="$SIPACUL_DEFAULT_ENVIRONMENT_FILE"
COMPOSE_PROJECT="$SIPACUL_DEFAULT_COMPOSE_PROJECT"
REGISTRY_OWNER="$SIPACUL_DEFAULT_REGISTRY_OWNER"
STATE_DIRECTORY="$SIPACUL_DEFAULT_STATE_DIRECTORY"
BACKUP_OUTPUT_DIRECTORY="$SIPACUL_DEFAULT_BACKUP_DIRECTORY"
HEALTH_TIMEOUT_SECONDS="$SIPACUL_DEFAULT_HEALTH_TIMEOUT_SECONDS"
EXECUTE=false
ALLOW_INITIAL_DEPLOYMENT_WITHOUT_BACKUP=false
PENDING_FILE=""
STAGE="preflight"
TEMP_RELEASE_FILE=""

usage() {
    cat <<'EOF'
Usage:
  sudo ./operations/linux/deploy.sh --release-sha <40-char-sha> [options]

Options:
  --release-sha SHA
  --repository-root PATH
  --environment-file PATH
  --compose-project NAME
  --registry-owner OWNER
  --state-directory PATH
  --backup-output-directory PATH
  --health-timeout-seconds N
  --execute
  --allow-initial-deployment-without-backup
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --release-sha) RELEASE_SHA="$2"; shift 2 ;;
        --repository-root) REPOSITORY_ROOT="$2"; shift 2 ;;
        --environment-file) ENVIRONMENT_FILE="$2"; shift 2 ;;
        --compose-project) COMPOSE_PROJECT="$2"; shift 2 ;;
        --registry-owner) REGISTRY_OWNER="$2"; shift 2 ;;
        --state-directory) STATE_DIRECTORY="$2"; shift 2 ;;
        --backup-output-directory) BACKUP_OUTPUT_DIRECTORY="$2"; shift 2 ;;
        --health-timeout-seconds) HEALTH_TIMEOUT_SECONDS="$2"; shift 2 ;;
        --execute) EXECUTE=true; shift ;;
        --allow-initial-deployment-without-backup)
            ALLOW_INITIAL_DEPLOYMENT_WITHOUT_BACKUP=true
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
        printf '\n[GAGAL] Deployment Linux berhenti pada stage %s.\n' "$STAGE" >&2
        printf '[AMAN] Tidak ada restore database atau schema downgrade otomatis.\n' >&2
        printf '[AMAN] Jika migration sudah dimulai, jangan rollback aplikasi sebelum kompatibilitas schema dikonfirmasi.\n' >&2
        [ -z "$PENDING_FILE" ] || printf '[INFO] Pending operation: %s\n' "$PENDING_FILE" >&2
    fi
    exit "$code"
}
trap on_exit EXIT

sipacul_require_root
for command_name in docker git python3 realpath; do
    sipacul_require_command "$command_name"
done
[ -n "$RELEASE_SHA" ] || sipacul_die "--release-sha wajib diisi."
TARGET_SHA="$(sipacul_normalize_sha "$RELEASE_SHA")"
sipacul_validate_registry_owner "$REGISTRY_OWNER"
REGISTRY_OWNER="$(printf '%s' "$REGISTRY_OWNER" | tr '[:upper:]' '[:lower:]')"
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
    sipacul_die "Pending deployment operation ditemukan: $PENDING_FILE. Investigasi harus diselesaikan dulu."

INITIAL=true
STATE_DATABASE_SHA=""
STATE_RUNTIME_SHA=""
STATE_PREVIOUS_RUNTIME_SHA=""
if [ -f "$CURRENT_FILE" ]; then
    INITIAL=false
    sipacul_assert_state_schema "$CURRENT_FILE"
    STATE_DATABASE_SHA="$(sipacul_json_get "$CURRENT_FILE" databaseReleaseSha)"
    STATE_RUNTIME_SHA="$(sipacul_json_get "$CURRENT_FILE" runtimeReleaseSha)"
    STATE_PREVIOUS_RUNTIME_SHA="$(sipacul_json_get "$CURRENT_FILE" previousRuntimeReleaseSha)"
    STATE_PROJECT="$(sipacul_json_get "$CURRENT_FILE" composeProject)"
    STATE_OWNER="$(sipacul_json_get "$CURRENT_FILE" registryOwner)"
    [ "$STATE_PROJECT" = "$COMPOSE_PROJECT" ] || sipacul_die "ComposeProject tidak cocok dengan deployment state."
    [ "$STATE_OWNER" = "$REGISTRY_OWNER" ] || sipacul_die "RegistryOwner tidak cocok dengan deployment state."
    [ -f "$CURRENT_RELEASE_FILE" ] || sipacul_die "current-release.env tidak ditemukan: $CURRENT_RELEASE_FILE"
    sipacul_assert_release_environment_matches_state "$CURRENT_RELEASE_FILE" "$CURRENT_FILE"
    if [ "$STATE_DATABASE_SHA" = "$TARGET_SHA" ] && [ "$STATE_RUNTIME_SHA" = "$TARGET_SHA" ]; then
        sipacul_die "Target release $TARGET_SHA sudah aktif."
    fi
else
    EXISTING_IDS="$(sipacul_project_container_ids)"
    [ -z "$EXISTING_IDS" ] || \
        sipacul_die "Container project $COMPOSE_PROJECT ada tetapi deployment state belum tersedia. Adopsi unmanaged stack ditolak."
fi

TEMP_RELEASE_FILE="$(mktemp /tmp/sipacul-release-plan.XXXXXX)"
sipacul_write_release_environment "$TEMP_RELEASE_FILE" "$TARGET_SHA" "$TARGET_SHA" "$REGISTRY_OWNER"
sipacul_compose "$TEMP_RELEASE_FILE" config --quiet

printf '=== PREFLIGHT DEPLOYMENT SIPACUL (LINUX) ===\n'
sipacul_ok "Repository: $REPOSITORY_ROOT"
sipacul_ok "Target full SHA: $TARGET_SHA"
sipacul_ok "Compose config target release valid."
sipacul_ok "State directory: $STATE_DIRECTORY"
sipacul_ok "Backup directory: $BACKUP_OUTPUT_DIRECTORY"
if [ "$INITIAL" = "true" ]; then
    sipacul_info "Deployment state belum ada; ini initial managed deployment."
else
    sipacul_info "Database release saat ini: $STATE_DATABASE_SHA"
    sipacul_info "Runtime release saat ini: $STATE_RUNTIME_SHA"
fi

printf '\n=== DEPLOYMENT PLAN ===\n'
printf '1. Pull dan verifikasi revision label empat immutable GHCR image.\n'
if [ "$INITIAL" = "true" ]; then
    printf '2. Initial deployment tidak memiliki database existing untuk dibackup.\n'
else
    printf '2. Pastikan PostgreSQL sehat lalu buat backup custom + SHA256 + manifest.\n'
fi
printf '3. Tulis pending operation dan release environment di luar repository.\n'
printf '4. Hentikan edge/frontend/API; PostgreSQL dan volume tetap dipertahankan.\n'
printf '5. Jalankan migrator target sebagai migration gate.\n'
printf '6. Mulai API, frontend, edge berurutan dan tunggu health check.\n'
printf '7. Simpan current deployment state/history; hapus pending operation.\n'
printf '[INFO] Tidak ada restore database atau rollback otomatis.\n'

if [ "$EXECUTE" != "true" ]; then
    rm -f "$TEMP_RELEASE_FILE"
    TEMP_RELEASE_FILE=""
    PENDING_FILE=""
    printf '\n=== STATUS AKHIR PLAN ===\n'
    sipacul_ok "Plan-only selesai; Docker runtime, image cache, database, state, dan secret tidak diubah."
    sipacul_ok "Jalankan ulang dengan --execute setelah plan disetujui."
    exit 0
fi

if [ "$INITIAL" = "true" ] && [ "$ALLOW_INITIAL_DEPLOYMENT_WITHOUT_BACKUP" != "true" ]; then
    sipacul_die "Initial deployment memerlukan --allow-initial-deployment-without-backup setelah memastikan instalasi benar-benar baru."
fi

rm -f "$TEMP_RELEASE_FILE"
TEMP_RELEASE_FILE=""

printf '\n=== PULL IMMUTABLE RELEASE IMAGES ===\n'
STAGE="pull-images"
MIGRATOR_IMAGE="$(sipacul_image_ref "$REGISTRY_OWNER" "migrator" "$TARGET_SHA")"
API_IMAGE="$(sipacul_image_ref "$REGISTRY_OWNER" "api" "$TARGET_SHA")"
FRONTEND_IMAGE="$(sipacul_image_ref "$REGISTRY_OWNER" "frontend" "$TARGET_SHA")"
EDGE_IMAGE="$(sipacul_image_ref "$REGISTRY_OWNER" "edge" "$TARGET_SHA")"
for pair in \
    "Migrator|$MIGRATOR_IMAGE" \
    "API|$API_IMAGE" \
    "Frontend|$FRONTEND_IMAGE" \
    "Edge|$EDGE_IMAGE"
do
    label="${pair%%|*}"
    image="${pair#*|}"
    sipacul_info "Pull $label: $image"
    sipacul_assert_image_revision "$image" "$TARGET_SHA"
    sipacul_ok "$label image revision cocok."
done

BACKUP_FILE=""
if [ "$INITIAL" != "true" ]; then
    printf '\n=== PRE-DEPLOY BACKUP ===\n'
    STAGE="pre-deploy-backup"
    sipacul_compose "$CURRENT_RELEASE_FILE" up --detach --no-build postgres >/dev/null
    sipacul_wait_service_healthy "$CURRENT_RELEASE_FILE" postgres "$HEALTH_TIMEOUT_SECONDS"
    sipacul_ok "PostgreSQL sehat."

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
    raise SystemExit(f"Backup pre-deploy harus menghasilkan tepat satu archive baru; aktual {len(new)}")
print(new[0])
PY
    )"
    [ -f "$BACKUP_FILE.sha256" ] && [ -f "$BACKUP_FILE.json" ] || \
        sipacul_die "Backup sidecar tidak lengkap: $BACKUP_FILE"
    sipacul_ok "Backup pre-deploy: $BACKUP_FILE"
fi

mkdir -p "$STATE_DIRECTORY/history"
chmod 750 "$STATE_DIRECTORY" "$STATE_DIRECTORY/history"

PREVIOUS_RUNTIME_SHA=""
if [ "$INITIAL" != "true" ]; then PREVIOUS_RUNTIME_SHA="$STATE_RUNTIME_SHA"; fi

STAGE="prepared"
python3 - \
    "$PENDING_FILE" \
    "$TARGET_SHA" \
    "$STATE_DATABASE_SHA" \
    "$PREVIOUS_RUNTIME_SHA" \
    "$BACKUP_FILE" \
    "$COMPOSE_PROJECT" \
    "$REGISTRY_OWNER" <<'PY'
import datetime
import json
import os
import sys

path, target, previous_db, previous_runtime, backup_file, project, owner = sys.argv[1:]
value = {
    "schemaVersion": 1,
    "application": "SiPacul",
    "operation": "deploy",
    "status": "in-progress",
    "stage": "prepared",
    "startedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "targetReleaseSha": target,
    "previousDatabaseReleaseSha": previous_db or None,
    "previousRuntimeReleaseSha": previous_runtime or None,
    "backupFile": backup_file or None,
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

sipacul_write_release_environment "$CURRENT_RELEASE_FILE" "$TARGET_SHA" "$TARGET_SHA" "$REGISTRY_OWNER"
sipacul_compose "$CURRENT_RELEASE_FILE" config --quiet
sipacul_ok "Release environment target aktif dan Compose config valid."

printf '\n=== POSTGRESQL ===\n'
STAGE="postgres"
sipacul_compose "$CURRENT_RELEASE_FILE" up --detach --no-build postgres >/dev/null
sipacul_wait_service_healthy "$CURRENT_RELEASE_FILE" postgres "$HEALTH_TIMEOUT_SECONDS"
sipacul_ok "PostgreSQL siap; volume database tetap dipertahankan."

printf '\n=== MAINTENANCE / MIGRATION GATE ===\n'
STAGE="stop-runtime"
sipacul_compose "$CURRENT_RELEASE_FILE" stop edge frontend api >/dev/null 2>&1 || true
sipacul_ok "Runtime application dihentikan; PostgreSQL tetap berjalan."

STAGE="migration"
sipacul_compose "$CURRENT_RELEASE_FILE" rm --force --stop migrator >/dev/null 2>&1 || true
sipacul_compose "$CURRENT_RELEASE_FILE" \
    up --no-deps --no-build \
    --abort-on-container-exit \
    --exit-code-from migrator \
    migrator >/dev/null
sipacul_ok "Migration gate selesai dengan exit code 0."

printf '\n=== START TARGET RUNTIME ===\n'
for service in api frontend edge; do
    STAGE="start-$service"
    sipacul_compose "$CURRENT_RELEASE_FILE" \
        up --detach --no-build --no-deps "$service" >/dev/null
    sipacul_wait_service_healthy "$CURRENT_RELEASE_FILE" "$service" "$HEALTH_TIMEOUT_SECONDS"
    sipacul_ok "$service sehat."
done

STAGE="finalize"
python3 - \
    "$CURRENT_FILE" \
    "$TARGET_SHA" \
    "$PREVIOUS_RUNTIME_SHA" \
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
    target,
    previous_runtime,
    owner,
    project,
    environment_file,
    release_environment_file,
    backup_file,
) = sys.argv[1:]
value = {
    "schemaVersion": 1,
    "application": "SiPacul",
    "status": "deployed",
    "databaseReleaseSha": target,
    "runtimeReleaseSha": target,
    "previousRuntimeReleaseSha": previous_runtime or None,
    "registryOwner": owner,
    "composeProject": project,
    "environmentFile": environment_file,
    "releaseEnvironmentFile": release_environment_file,
    "backupFile": backup_file or None,
    "deployedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
}
tmp = path + ".tmp"
with open(tmp, "w", encoding="utf-8", newline="\n") as handle:
    json.dump(value, handle, indent=2)
    handle.write("\n")
os.chmod(tmp, 0o600)
os.replace(tmp, path)
PY

HISTORY_PATH="$(sipacul_write_history_copy "$STATE_DIRECTORY" "$CURRENT_FILE" deploy "$TARGET_SHA")"
rm -f "$PENDING_FILE"
PENDING_FILE=""

printf '\n=== STATUS AKHIR DEPLOYMENT ===\n'
sipacul_ok "Database release: $TARGET_SHA"
sipacul_ok "Runtime release: $TARGET_SHA"
sipacul_ok "Deployment state: $CURRENT_FILE"
sipacul_ok "Deployment history: $HISTORY_PATH"
[ -z "$BACKUP_FILE" ] || sipacul_ok "Backup pre-deploy dipertahankan: $BACKUP_FILE"
sipacul_ok "Tidak ada database restore, volume deletion, atau rollback otomatis."
