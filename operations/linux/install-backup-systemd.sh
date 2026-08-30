#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=sipacul-common.sh
. "$SCRIPT_DIR/sipacul-common.sh"

REPOSITORY_ROOT="$SIPACUL_DEFAULT_REPOSITORY_ROOT"
EXECUTE=false
ENABLE=false
FORCE=false
SERVICE_NAME="sipacul-postgres-backup.service"
TIMER_NAME="sipacul-postgres-backup.timer"
SYSTEMD_DIRECTORY="/etc/systemd/system"

usage() {
    cat <<'EOF'
Usage:
  sudo ./operations/linux/install-backup-systemd.sh [options]

Options:
  --repository-root PATH
  --execute
  --enable
  --force

Default is plan-only. --enable requires --execute.
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --repository-root) REPOSITORY_ROOT="$2"; shift 2 ;;
        --execute) EXECUTE=true; shift ;;
        --enable) ENABLE=true; shift ;;
        --force) FORCE=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) sipacul_die "Argument tidak dikenal: $1" ;;
    esac
done

sipacul_require_root
for command_name in cmp install realpath systemctl systemd-analyze; do
    sipacul_require_command "$command_name"
done

[ "$ENABLE" = "false" ] || [ "$EXECUTE" = "true" ] || sipacul_die "--enable membutuhkan --execute."
sipacul_resolve_repository_root "$REPOSITORY_ROOT"
sipacul_assert_git_clean

SOURCE_SERVICE="$REPOSITORY_ROOT/operations/linux/systemd/$SERVICE_NAME"
SOURCE_TIMER="$REPOSITORY_ROOT/operations/linux/systemd/$TIMER_NAME"
[ -f "$SOURCE_SERVICE" ] || sipacul_die "Source service tidak ditemukan: $SOURCE_SERVICE"
[ -f "$SOURCE_TIMER" ] || sipacul_die "Source timer tidak ditemukan: $SOURCE_TIMER"

systemd-analyze verify "$SOURCE_SERVICE" "$SOURCE_TIMER"

TARGET_SERVICE="$SYSTEMD_DIRECTORY/$SERVICE_NAME"
TARGET_TIMER="$SYSTEMD_DIRECTORY/$TIMER_NAME"

for pair in "$SOURCE_SERVICE:$TARGET_SERVICE" "$SOURCE_TIMER:$TARGET_TIMER"; do
    source_path="${pair%%:*}"
    target_path="${pair#*:}"
    if [ -e "$target_path" ] && ! cmp -s "$source_path" "$target_path"; then
        [ "$FORCE" = "true" ] || sipacul_die "Unit existing berbeda: $target_path. Gunakan --force setelah audit."
    fi
done

printf '=== PLAN SYSTEMD BACKUP SIPACUL ===\n'
sipacul_ok "Service source: $SOURCE_SERVICE"
sipacul_ok "Timer source: $SOURCE_TIMER"
sipacul_ok "Schedule: 02:00 Asia/Jakarta; Persistent=true."
sipacul_ok "Timer activation requested: $ENABLE"

if [ "$EXECUTE" != "true" ]; then
    printf '\n=== STATUS AKHIR PLAN ===\n'
    sipacul_ok "Plan-only selesai; /etc/systemd/system dan timer tidak diubah."
    exit 0
fi

printf '\n=== INSTALL SYSTEMD UNITS ===\n'
TMP_SERVICE="$TARGET_SERVICE.sipacul-tmp-$$"
TMP_TIMER="$TARGET_TIMER.sipacul-tmp-$$"
trap 'rm -f "$TMP_SERVICE" "$TMP_TIMER"' EXIT

install -m 0644 -o root -g root "$SOURCE_SERVICE" "$TMP_SERVICE"
install -m 0644 -o root -g root "$SOURCE_TIMER" "$TMP_TIMER"
mv -f "$TMP_SERVICE" "$TARGET_SERVICE"
mv -f "$TMP_TIMER" "$TARGET_TIMER"
systemctl daemon-reload

cmp -s "$SOURCE_SERVICE" "$TARGET_SERVICE" || sipacul_die "Installed service berbeda dari source."
cmp -s "$SOURCE_TIMER" "$TARGET_TIMER" || sipacul_die "Installed timer berbeda dari source."
sipacul_ok "Service dan timer terpasang identik dengan repository."

if [ "$ENABLE" = "true" ]; then
    systemctl enable --now "$TIMER_NAME"
    systemctl is-enabled "$TIMER_NAME"
    systemctl is-active "$TIMER_NAME"
    sipacul_ok "Timer enabled dan active."
else
    sipacul_info "Timer tidak di-enable oleh operasi ini. Aktifkan setelah private initial deployment dan backup manual lulus."
fi

printf '\n=== STATUS AKHIR INSTALL SYSTEMD ===\n'
sipacul_ok "systemd units terpasang; database, container, DNS, firewall, certificate, dan public bind tidak diubah."
