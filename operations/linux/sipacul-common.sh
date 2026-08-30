#!/usr/bin/env bash
set -Eeuo pipefail

SIPACUL_DEFAULT_REPOSITORY_ROOT="/opt/sipacul/repository"
SIPACUL_DEFAULT_ENVIRONMENT_FILE="/etc/sipacul/.env.production"
SIPACUL_DEFAULT_STATE_DIRECTORY="/var/lib/sipacul/deployment-state"
SIPACUL_DEFAULT_BACKUP_DIRECTORY="/var/backups/sipacul"
SIPACUL_DEFAULT_COMPOSE_PROJECT="sipacul-production"
SIPACUL_DEFAULT_REGISTRY_OWNER="rizkyerapee-bit"
SIPACUL_DEFAULT_HEALTH_TIMEOUT_SECONDS="180"

sipacul_die() {
    printf '[GAGAL] %s\n' "$*" >&2
    exit 1
}

sipacul_info() {
    printf '[INFO] %s\n' "$*"
}

sipacul_ok() {
    printf '[OK] %s\n' "$*"
}

sipacul_require_root() {
    [ "$(id -u)" -eq 0 ] || sipacul_die "Jalankan operasi production dengan sudo/root."
}

sipacul_require_command() {
    command -v "$1" >/dev/null 2>&1 || sipacul_die "Command tidak ditemukan: $1"
}

sipacul_normalize_sha() {
    local value
    value="$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')"
    case "$value" in
        *[!0-9a-f]*|'') sipacul_die "Release SHA harus full Git SHA 40 karakter hexadecimal." ;;
    esac
    [ "${#value}" -eq 40 ] || sipacul_die "Release SHA harus full Git SHA 40 karakter hexadecimal."
    printf '%s\n' "$value"
}

sipacul_validate_registry_owner() {
    case "$1" in
        ''|*[!a-zA-Z0-9._-]*)
            sipacul_die "Registry owner tidak valid: $1"
            ;;
    esac
    case "$1" in
        [a-zA-Z0-9]*) ;;
        *) sipacul_die "Registry owner tidak valid: $1" ;;
    esac
}

sipacul_image_ref() {
    local owner component sha
    owner="$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')"
    component="$2"
    sha="$(sipacul_normalize_sha "$3")"
    printf 'ghcr.io/%s/sipacul-%s:sha-%s\n' "$owner" "$component" "$sha"
}

sipacul_resolve_repository_root() {
    local requested="$1"
    [ -n "$requested" ] || requested="$SIPACUL_DEFAULT_REPOSITORY_ROOT"
    [ -d "$requested" ] || sipacul_die "Repository tidak ditemukan: $requested"
    REPOSITORY_ROOT="$(realpath "$requested")"
    [ -f "$REPOSITORY_ROOT/compose.production.yml" ] || \
        sipacul_die "compose.production.yml tidak ditemukan di repository: $REPOSITORY_ROOT"
    COMPOSE_FILE="$REPOSITORY_ROOT/compose.production.yml"
}

sipacul_resolve_file() {
    local value="$1" base="$2" label="$3"
    [ -n "$value" ] || sipacul_die "$label tidak boleh kosong."
    if [ "${value#/}" = "$value" ]; then
        value="$base/$value"
    fi
    value="$(realpath -m "$value")"
    [ -f "$value" ] || sipacul_die "$label tidak ditemukan: $value"
    printf '%s\n' "$value"
}

sipacul_resolve_directory() {
    local value="$1" base="$2"
    [ -n "$value" ] || sipacul_die "Path directory tidak boleh kosong."
    if [ "${value#/}" = "$value" ]; then
        value="$base/$value"
    fi
    realpath -m "$value"
}

sipacul_assert_outside_repository() {
    local candidate root label
    candidate="$(realpath -m "$1")"
    root="$(realpath "$REPOSITORY_ROOT")"
    label="$2"
    case "$candidate/" in
        "$root/"*) sipacul_die "$label wajib berada di luar repository: $candidate" ;;
    esac
}

sipacul_git() {
    git -c "safe.directory=$REPOSITORY_ROOT" -C "$REPOSITORY_ROOT" "$@"
}

sipacul_assert_git_clean() {
    local status
    status="$(sipacul_git status --porcelain=v1 --untracked-files=all)"
    [ -z "$status" ] || sipacul_die "Working tree/staging repository tidak bersih."
}

sipacul_assert_production_environment() {
    local path="$1"
    python3 - "$path" <<'PY'
import os
import re
import sys

path = sys.argv[1]
required = [
    "POSTGRES_DB",
    "POSTGRES_USER",
    "POSTGRES_PASSWORD",
    "SIPACUL_BOOTSTRAP_OWNER_TOKEN",
    "SIPACUL_BIND_ADDRESS",
    "SIPACUL_HTTPS_PORT",
    "SIPACUL_APPLICATION_SUBNET",
    "SIPACUL_EDGE_IP",
    "SIPACUL_TLS_CERTIFICATE_PATH",
    "SIPACUL_TLS_PRIVATE_KEY_PATH",
]
pattern = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*)=(.*)$")
values = {}
with open(path, "r", encoding="utf-8") as handle:
    for raw in handle:
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        match = pattern.match(line)
        if not match:
            raise SystemExit(f"Baris environment tidak valid pada {path}: {raw.rstrip()}")
        name, value = match.groups()
        if name in values:
            raise SystemExit(f"Environment variable duplikat pada {path}: {name}")
        values[name] = value

for name in required:
    value = values.get(name, "")
    if not value.strip():
        raise SystemExit(f"Environment variable wajib tidak tersedia: {name}")
    if "REPLACE_ME" in value:
        raise SystemExit(f"Environment variable masih memakai placeholder: {name}")

for name in ("SIPACUL_TLS_CERTIFICATE_PATH", "SIPACUL_TLS_PRIVATE_KEY_PATH"):
    value = values[name]
    if not os.path.isabs(value):
        raise SystemExit(f"{name} harus berupa path absolut.")
    if not os.path.isfile(value):
        raise SystemExit(f"{name} tidak ditemukan: {value}")
PY
}

sipacul_env_value_from_file() {
    local path="$1" key="$2"
    python3 - "$path" "$key" <<'PY'
import re
import sys

path, key = sys.argv[1], sys.argv[2]
pattern = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*)=(.*)$")
found = []
with open(path, "r", encoding="utf-8") as handle:
    for raw in handle:
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        match = pattern.match(line)
        if match and match.group(1) == key:
            found.append(match.group(2))
if len(found) != 1:
    raise SystemExit(f"{key} harus muncul tepat satu kali pada {path}")
print(found[0])
PY
}

sipacul_write_release_environment() {
    local path="$1" database_sha="$2" runtime_sha="$3" owner="$4"
    local dir
    database_sha="$(sipacul_normalize_sha "$database_sha")"
    runtime_sha="$(sipacul_normalize_sha "$runtime_sha")"
    sipacul_validate_registry_owner "$owner"
    owner="$(printf '%s' "$owner" | tr '[:upper:]' '[:lower:]')"
    dir="$(dirname "$path")"
    mkdir -p "$dir"
    umask 077
    cat >"$path.tmp.$$" <<EOF
# Managed by SiPacul Linux deployment operations. Contains image references only.
# Database release SHA: $database_sha
# Runtime release SHA: $runtime_sha
SIPACUL_MIGRATOR_IMAGE=$(sipacul_image_ref "$owner" "migrator" "$database_sha")
SIPACUL_API_IMAGE=$(sipacul_image_ref "$owner" "api" "$runtime_sha")
SIPACUL_FRONTEND_IMAGE=$(sipacul_image_ref "$owner" "frontend" "$runtime_sha")
SIPACUL_EDGE_IMAGE=$(sipacul_image_ref "$owner" "edge" "$runtime_sha")
EOF
    chmod 600 "$path.tmp.$$"
    mv -f "$path.tmp.$$" "$path"
}

sipacul_compose() {
    local release_file="$1"
    shift
    docker compose \
        --env-file "$ENVIRONMENT_FILE" \
        --env-file "$release_file" \
        --file "$COMPOSE_FILE" \
        --project-name "$COMPOSE_PROJECT" \
        "$@"
}

sipacul_project_container_ids() {
    docker ps --all \
        --filter "label=com.docker.compose.project=$COMPOSE_PROJECT" \
        --format '{{.ID}}'
}

sipacul_service_container_id() {
    local release_file="$1" service="$2"
    local ids
    ids="$(sipacul_compose "$release_file" ps --all --quiet "$service")"
    if [ -z "$ids" ]; then
        printf '\n'
        return
    fi
    [ "$(printf '%s\n' "$ids" | sed '/^$/d' | wc -l)" -eq 1 ] || \
        sipacul_die "Service $service memiliki lebih dari satu container."
    printf '%s\n' "$ids"
}

sipacul_wait_service_healthy() {
    local release_file="$1" service="$2" timeout="$3"
    local started now id state
    started="$(date +%s)"
    while :; do
        id="$(sipacul_service_container_id "$release_file" "$service")"
        if [ -n "$id" ]; then
            state="$(docker inspect \
                --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' \
                "$id")"
            case "$state" in
                healthy|running) return 0 ;;
                unhealthy|dead|exited)
                    sipacul_die "Service $service masuk state $state sebelum sehat."
                    ;;
            esac
        fi
        now="$(date +%s)"
        [ $((now - started)) -lt "$timeout" ] || \
            sipacul_die "Service $service tidak sehat dalam $timeout detik."
        sleep 2
    done
}

sipacul_assert_image_revision() {
    local image="$1" expected="$2" actual
    expected="$(sipacul_normalize_sha "$expected")"
    docker pull "$image" >/dev/null
    actual="$(docker image inspect \
        --format '{{ index .Config.Labels "org.opencontainers.image.revision" }}' \
        "$image")"
    actual="$(printf '%s' "$actual" | tr '[:upper:]' '[:lower:]')"
    [ "$actual" = "$expected" ] || \
        sipacul_die "Image revision tidak cocok untuk $image. Aktual=$actual; expected=$expected"
}

sipacul_json_get() {
    local path="$1" key="$2"
    python3 - "$path" "$key" <<'PY'
import json
import sys
with open(sys.argv[1], "r", encoding="utf-8") as handle:
    value = json.load(handle).get(sys.argv[2])
if value is None:
    print("")
elif isinstance(value, bool):
    print("true" if value else "false")
else:
    print(value)
PY
}

sipacul_assert_state_schema() {
    local path="$1"
    python3 - "$path" <<'PY'
import json
import re
import sys

path = sys.argv[1]
with open(path, "r", encoding="utf-8") as handle:
    state = json.load(handle)
required = [
    "schemaVersion",
    "application",
    "databaseReleaseSha",
    "runtimeReleaseSha",
    "registryOwner",
    "composeProject",
]
for name in required:
    if name not in state:
        raise SystemExit(f"Deployment state kehilangan properti: {name}")
if state["schemaVersion"] != 1 or state["application"] != "SiPacul":
    raise SystemExit("Deployment state tidak didukung.")
sha = re.compile(r"^[0-9a-fA-F]{40}$")
for name in ("databaseReleaseSha", "runtimeReleaseSha"):
    if not sha.match(str(state[name])):
        raise SystemExit(f"Deployment state memiliki SHA tidak valid: {name}")
previous = state.get("previousRuntimeReleaseSha")
if previous not in (None, "") and not sha.match(str(previous)):
    raise SystemExit("previousRuntimeReleaseSha tidak valid.")
PY
}

sipacul_assert_release_environment_matches_state() {
    local release_file="$1" state_file="$2"
    local db runtime owner expected actual key
    sipacul_assert_state_schema "$state_file"
    db="$(sipacul_json_get "$state_file" databaseReleaseSha)"
    runtime="$(sipacul_json_get "$state_file" runtimeReleaseSha)"
    owner="$(sipacul_json_get "$state_file" registryOwner)"
    for key in MIGRATOR API FRONTEND EDGE; do
        case "$key" in
            MIGRATOR) expected="$(sipacul_image_ref "$owner" "migrator" "$db")" ;;
            API) expected="$(sipacul_image_ref "$owner" "api" "$runtime")" ;;
            FRONTEND) expected="$(sipacul_image_ref "$owner" "frontend" "$runtime")" ;;
            EDGE) expected="$(sipacul_image_ref "$owner" "edge" "$runtime")" ;;
        esac
        actual="$(sipacul_env_value_from_file "$release_file" "SIPACUL_${key}_IMAGE")"
        [ "$actual" = "$expected" ] || \
            sipacul_die "Release environment tidak cocok dengan deployment state pada SIPACUL_${key}_IMAGE."
    done
}

sipacul_write_history_copy() {
    local state_dir="$1" state_file="$2" operation="$3" runtime_sha="$4"
    local history stamp short target
    history="$state_dir/history"
    mkdir -p "$history"
    stamp="$(date -u +%Y%m%dT%H%M%S%3NZ)"
    short="${runtime_sha:0:12}"
    target="$history/$stamp-$operation-$short.json"
    cp "$state_file" "$target"
    chmod 600 "$target"
    printf '%s\n' "$target"
}
