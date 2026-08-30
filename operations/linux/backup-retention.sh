#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=sipacul-common.sh
. "$SCRIPT_DIR/sipacul-common.sh"

BACKUP_DIRECTORY="$SIPACUL_DEFAULT_BACKUP_DIRECTORY"
RETENTION_DAYS=30
MINIMUM_BACKUPS=7
APPLY=false

usage() {
    cat <<'EOF'
Usage:
  sudo ./operations/linux/backup-retention.sh [options]

Options:
  --backup-directory PATH
  --retention-days DAYS
  --minimum-backups COUNT
  --apply
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --backup-directory) BACKUP_DIRECTORY="$2"; shift 2 ;;
        --retention-days) RETENTION_DAYS="$2"; shift 2 ;;
        --minimum-backups) MINIMUM_BACKUPS="$2"; shift 2 ;;
        --apply) APPLY=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) sipacul_die "Argument tidak dikenal: $1" ;;
    esac
done

sipacul_require_command python3
[ -f "$SCRIPT_DIR/sipacul-backup-set.py" ] || sipacul_die "Backup set helper tidak ditemukan."

printf '=== PREFLIGHT RETENSI BACKUP SIPACUL (LINUX) ===\n'
args=(
    retention
    --directory "$BACKUP_DIRECTORY"
    --retention-days "$RETENTION_DAYS"
    --minimum-backups "$MINIMUM_BACKUPS"
)
if [ "$APPLY" = "true" ]; then
    args+=(--apply)
fi
python3 "$SCRIPT_DIR/sipacul-backup-set.py" "${args[@]}"

printf '\n=== STATUS AKHIR RETENSI ===\n'
if [ "$APPLY" = "true" ]; then
    sipacul_ok "Retensi selesai dalam mode APPLY."
else
    sipacul_ok "Retensi selesai dalam mode DRY-RUN; tidak ada penghapusan."
fi
