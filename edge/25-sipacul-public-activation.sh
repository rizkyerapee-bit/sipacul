#!/bin/sh
set -eu

activation="${SIPACUL_PUBLIC_ACTIVATION:-disabled}"
hostname="${SIPACUL_PUBLIC_HOSTNAME:-_}"
hsts_enabled="${SIPACUL_HSTS_ENABLED:-false}"
hsts_max_age="${SIPACUL_HSTS_MAX_AGE:-86400}"
bind_address="${SIPACUL_BIND_ADDRESS:-127.0.0.1}"
https_port="${SIPACUL_HTTPS_PORT:-8443}"
snippet="/etc/nginx/snippets/sipacul-hsts.conf"

fail() {
    echo "[sipacul-public-activation] ERROR: $*" >&2
    exit 1
}

case "$activation" in
    disabled)
        [ "$hostname" = "_" ] || fail "disabled activation requires SIPACUL_PUBLIC_HOSTNAME=_"
        [ "$hsts_enabled" = "false" ] || fail "HSTS must remain false while public activation is disabled"
        [ "$bind_address" = "127.0.0.1" ] || fail "disabled activation requires loopback bind 127.0.0.1"
        : > "$snippet"
        ;;
    enabled)
        [ "$hostname" != "_" ] || fail "enabled activation requires a public hostname"
        [ "$hostname" != "localhost" ] || fail "localhost is not a public hostname"

        if ! printf '%s\n' "$hostname" | grep -Eq '^[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?(\.[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?)+$'; then
            fail "SIPACUL_PUBLIC_HOSTNAME must be a DNS hostname"
        fi

        [ "$bind_address" != "127.0.0.1" ] || fail "enabled activation requires a non-loopback bind"
        [ "$https_port" = "443" ] || fail "enabled activation requires SIPACUL_HTTPS_PORT=443"

        case "$hsts_enabled" in
            false)
                : > "$snippet"
                ;;
            true)
                case "$hsts_max_age" in
                    ''|*[!0-9]*)
                        fail "SIPACUL_HSTS_MAX_AGE must be an integer"
                        ;;
                esac

                if [ "$hsts_max_age" -lt 300 ] || [ "$hsts_max_age" -gt 63072000 ]; then
                    fail "SIPACUL_HSTS_MAX_AGE must be between 300 and 63072000 seconds"
                fi

                printf 'add_header Strict-Transport-Security "max-age=%s" always;\n' "$hsts_max_age" > "$snippet"
                ;;
            *)
                fail "SIPACUL_HSTS_ENABLED must be true or false"
                ;;
        esac
        ;;
    *)
        fail "SIPACUL_PUBLIC_ACTIVATION must be disabled or enabled"
        ;;
esac

echo "[sipacul-public-activation] activation=$activation hostname=$hostname hsts=$hsts_enabled"
