#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=sipacul-common.sh
. "$SCRIPT_DIR/sipacul-common.sh"

REPOSITORY_ROOT="$SIPACUL_DEFAULT_REPOSITORY_ROOT"
ENVIRONMENT_FILE="$SIPACUL_DEFAULT_ENVIRONMENT_FILE"
COMPOSE_PROJECT="$SIPACUL_DEFAULT_COMPOSE_PROJECT"
OUTPUT_DIRECTORY="$SIPACUL_DEFAULT_BACKUP_DIRECTORY"
LOG_FILE="/var/log/sipacul/backup-cycle.log"
RETENTION_DAYS=30
MINIMUM_BACKUPS=7
FRESHNESS_HOURS=26
APPLY_RETENTION=false
VERIFY_ALL_HASHES=false
CYCLE_LOCK_FILE="${SIPACUL_BACKUP_CYCLE_LOCK_FILE:-/run/lock/sipacul-postgres-backup-cycle.lock}"

usage() {
    cat <<'EOF'
Usage:
  sudo ./operations/linux/backup-cycle.sh [options]

Options:
  --repository-root PATH
  --environment-file PATH
  --compose-project NAME
  --output-directory PATH
  --log-file PATH
  --retention-days DAYS
  --minimum-backups COUNT
  --freshness-hours HOURS
  --apply-retention
  --verify-all-hashes
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --repository-root) REPOSITORY_ROOT="$2"; shift 2 ;;
        --environment-file) ENVIRONMENT_FILE="$2"; shift 2 ;;
        --compose-project) COMPOSE_PROJECT="$2"; shift 2 ;;
        --output-directory) OUTPUT_DIRECTORY="$2"; shift 2 ;;
        --log-file) LOG_FILE="$2"; shift 2 ;;
        --retention-days) RETENTION_DAYS="$2"; shift 2 ;;
        --minimum-backups) MINIMUM_BACKUPS="$2"; shift 2 ;;
        --freshness-hours) FRESHNESS_HOURS="$2"; shift 2 ;;
        --apply-retention) APPLY_RETENTION=true; shift ;;
        --verify-all-hashes) VERIFY_ALL_HASHES=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) sipacul_die "Argument tidak dikenal: $1" ;;
    esac
done

sipacul_require_root
for command_name in flock git realpath tee; do
    sipacul_require_command "$command_name"
done

sipacul_resolve_repository_root "$REPOSITORY_ROOT"
ENVIRONMENT_FILE="$(sipacul_resolve_file "$ENVIRONMENT_FILE" "$REPOSITORY_ROOT" "Production environment")"
OUTPUT_DIRECTORY="$(sipacul_resolve_directory "$OUTPUT_DIRECTORY" "$REPOSITORY_ROOT")"
LOG_FILE="$(realpath -m "$LOG_FILE")"
sipacul_assert_outside_repository "$OUTPUT_DIRECTORY" "OutputDirectory"
sipacul_assert_outside_repository "$LOG_FILE" "LogFile"
sipacul_assert_production_environment "$ENVIRONMENT_FILE"
sipacul_assert_git_clean

mkdir -p "$OUTPUT_DIRECTORY" "$(dirname "$LOG_FILE")" "$(dirname "$CYCLE_LOCK_FILE")"
chmod 750 "$OUTPUT_DIRECTORY" "$(dirname "$LOG_FILE")"
touch "$LOG_FILE"
chmod 600 "$LOG_FILE"

exec 9>"$CYCLE_LOCK_FILE"
if ! flock -n 9; then
    sipacul_die "Siklus backup lain sedang berjalan."
fi

GIT_HEAD_BEFORE="$(sipacul_git rev-parse HEAD)"
GIT_STATUS_BEFORE="$(sipacul_git status --porcelain=v1 --untracked-files=all)"

log_line() {
    printf '%s %s\n' "$(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)" "$*" >>"$LOG_FILE"
}

run_stage() {
    local stage="$1"
    shift
    log_line "stage=$stage event=start"
    if "$@" 2>&1 | tee -a "$LOG_FILE"; then
        log_line "stage=$stage event=completed exitCode=0"
    else
        local code=$?
        log_line "stage=$stage event=failed exitCode=$code"
        return "$code"
    fi
}

log_line "cycle=start head=$GIT_HEAD_BEFORE"
printf '=== PREFLIGHT SIKLUS BACKUP SIPACUL (LINUX) ===\n'
sipacul_ok "Repository: $REPOSITORY_ROOT"
sipacul_ok "Lock eksklusif diperoleh: $CYCLE_LOCK_FILE"
sipacul_ok "Output: $OUTPUT_DIRECTORY"
sipacul_ok "Log: $LOG_FILE"
sipacul_ok "Retensi: $RETENTION_DAYS hari / minimum $MINIMUM_BACKUPS / apply=$APPLY_RETENTION"

backup_args=(
    --repository-root "$REPOSITORY_ROOT"
    --environment-file "$ENVIRONMENT_FILE"
    --compose-project "$COMPOSE_PROJECT"
    --output-directory "$OUTPUT_DIRECTORY"
)

printf '\n=== MEMBUAT BACKUP TERJADWAL ===\n'
run_stage backup "$SCRIPT_DIR/backup-postgres.sh" "${backup_args[@]}"

retention_args=(
    --backup-directory "$OUTPUT_DIRECTORY"
    --retention-days "$RETENTION_DAYS"
    --minimum-backups "$MINIMUM_BACKUPS"
)
if [ "$APPLY_RETENTION" = "true" ]; then
    retention_args+=(--apply)
fi

printf '\n=== MENJALANKAN RETENSI ===\n'
run_stage retention "$SCRIPT_DIR/backup-retention.sh" "${retention_args[@]}"

freshness_args=(
    --backup-directory "$OUTPUT_DIRECTORY"
    --max-age-hours "$FRESHNESS_HOURS"
    --minimum-valid-backups 1
)
if [ "$VERIFY_ALL_HASHES" = "true" ]; then
    freshness_args+=(--verify-all-hashes)
fi

printf '\n=== MEMERIKSA FRESHNESS ===\n'
run_stage freshness "$SCRIPT_DIR/backup-freshness.sh" "${freshness_args[@]}"

GIT_HEAD_AFTER="$(sipacul_git rev-parse HEAD)"
GIT_STATUS_AFTER="$(sipacul_git status --porcelain=v1 --untracked-files=all)"
[ "$GIT_HEAD_AFTER" = "$GIT_HEAD_BEFORE" ] || sipacul_die "Git HEAD berubah selama siklus backup."
[ "$GIT_STATUS_AFTER" = "$GIT_STATUS_BEFORE" ] || sipacul_die "Working tree berubah selama siklus backup."

log_line "cycle=completed head=$GIT_HEAD_AFTER"
printf '\n=== STATUS AKHIR SIKLUS BACKUP ===\n'
sipacul_ok "Backup, retensi, freshness, lock, dan log operasional lulus."
sipacul_ok "HEAD dan working tree tidak berubah."
sipacul_ok "Log: $LOG_FILE"
