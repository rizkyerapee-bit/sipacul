#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=sipacul-common.sh
. "$SCRIPT_DIR/sipacul-common.sh"

BACKUP_DIRECTORY="$SIPACUL_DEFAULT_BACKUP_DIRECTORY"
MAX_AGE_HOURS=26
MINIMUM_VALID_BACKUPS=1
VERIFY_ALL_HASHES=false

usage() {
    cat <<'EOF'
Usage:
  sudo ./operations/linux/backup-freshness.sh [options]

Options:
  --backup-directory PATH
  --max-age-hours HOURS
  --minimum-valid-backups COUNT
  --verify-all-hashes
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --backup-directory) BACKUP_DIRECTORY="$2"; shift 2 ;;
        --max-age-hours) MAX_AGE_HOURS="$2"; shift 2 ;;
        --minimum-valid-backups) MINIMUM_VALID_BACKUPS="$2"; shift 2 ;;
        --verify-all-hashes) VERIFY_ALL_HASHES=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) sipacul_die "Argument tidak dikenal: $1" ;;
    esac
done

sipacul_require_command python3
[ -f "$SCRIPT_DIR/sipacul-backup-set.py" ] || sipacul_die "Backup set helper tidak ditemukan."

printf '=== PREFLIGHT BACKUP FRESHNESS SIPACUL (LINUX) ===\n'
args=(
    freshness
    --directory "$BACKUP_DIRECTORY"
    --max-age-hours "$MAX_AGE_HOURS"
    --minimum-valid-backups "$MINIMUM_VALID_BACKUPS"
)
if [ "$VERIFY_ALL_HASHES" = "true" ]; then
    args+=(--verify-all-hashes)
fi
python3 "$SCRIPT_DIR/sipacul-backup-set.py" "${args[@]}"

printf '\n=== STATUS AKHIR BACKUP FRESHNESS ===\n'
sipacul_ok "Freshness dan integrity backup lulus."
