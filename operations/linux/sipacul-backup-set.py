#!/usr/bin/env python3
import argparse
import datetime as dt
import hashlib
import json
import os
import re
import shutil
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

DUMP_RE = re.compile(r"^sipacul-postgres-(\d{8}T\d{9}Z)\.dump$")
SIDE_RE = re.compile(r"^sipacul-postgres-(\d{8}T\d{9}Z)\.dump\.(sha256|json)$")
SHA_LINE_RE = re.compile(r"^([0-9A-Fa-f]{64})\s+\*?(.+)$")
SHA_RE = re.compile(r"^[0-9A-Fa-f]{64}$")
TIMESTAMP_FORMAT = "%Y%m%dT%H%M%S%fZ"
INCOMPLETE_GRACE_SECONDS = 300


class BackupError(RuntimeError):
    pass


@dataclass(frozen=True)
class BackupSet:
    dump: Path
    checksum: Path
    manifest: Path
    created_utc: dt.datetime
    manifest_created_utc: dt.datetime
    database: str
    latest_migration: str
    size_bytes: int
    sha256: str


def fail(message: str) -> None:
    raise BackupError(message)


def utc_now() -> dt.datetime:
    return dt.datetime.now(dt.timezone.utc)


def parse_timestamp(text: str) -> dt.datetime:
    try:
        value = dt.datetime.strptime(text, TIMESTAMP_FORMAT)
    except ValueError as exc:
        fail(f"Timestamp nama archive tidak valid: {text}: {exc}")
    return value.replace(tzinfo=dt.timezone.utc)


def parse_iso_utc(value: object, label: str) -> dt.datetime:
    if not isinstance(value, str) or not value.strip():
        fail(f"{label} tidak valid.")
    text = value.strip()
    if text.endswith("Z"):
        text = text[:-1] + "+00:00"
    try:
        parsed = dt.datetime.fromisoformat(text)
    except ValueError as exc:
        fail(f"{label} tidak valid: {exc}")
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=dt.timezone.utc)
    return parsed.astimezone(dt.timezone.utc)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def is_recent(path: Path, now: dt.datetime) -> bool:
    modified = dt.datetime.fromtimestamp(path.stat().st_mtime, tz=dt.timezone.utc)
    age = (now - modified).total_seconds()
    return -300 <= age <= INCOMPLETE_GRACE_SECONDS


def resolve_directory(raw: str) -> Path:
    path = Path(raw).expanduser().resolve()
    if not path.is_dir():
        fail(f"Folder backup tidak ditemukan: {path}")
    if path == Path(path.anchor):
        fail("Folder backup tidak boleh berupa root filesystem.")
    return path


def scan_backup_sets(directory: Path) -> list[BackupSet]:
    now = utc_now()
    prefixed = sorted(p for p in directory.iterdir() if p.is_file() and p.name.startswith("sipacul-postgres-"))
    by_base: dict[str, dict[str, Path]] = {}
    unknown: list[Path] = []

    for path in prefixed:
        if ".partial-" in path.name:
            continue
        dump_match = DUMP_RE.match(path.name)
        if dump_match:
            base = path.name
            by_base.setdefault(base, {})["dump"] = path
            continue
        side_match = SIDE_RE.match(path.name)
        if side_match:
            base = f"sipacul-postgres-{side_match.group(1)}.dump"
            by_base.setdefault(base, {})[side_match.group(2)] = path
            continue
        unknown.append(path)

    if unknown:
        fail("File backup tidak dikenali: " + ", ".join(p.name for p in unknown))

    results: list[BackupSet] = []
    for base_name, parts in sorted(by_base.items()):
        required = {"dump", "sha256", "json"}
        missing = required.difference(parts)
        if missing:
            present = list(parts.values())
            if present and all(is_recent(path, now) for path in present):
                continue
            fail(f"Backup set tidak lengkap untuk {base_name}; missing: {', '.join(sorted(missing))}")

        dump = parts["dump"]
        checksum = parts["sha256"]
        manifest_path = parts["json"]
        match = DUMP_RE.match(dump.name)
        assert match is not None
        created_utc = parse_timestamp(match.group(1))

        try:
            with manifest_path.open("r", encoding="utf-8") as handle:
                manifest = json.load(handle)
        except (OSError, json.JSONDecodeError) as exc:
            fail(f"Manifest JSON tidak valid untuk {dump.name}: {exc}")

        required_manifest = [
            "schemaVersion",
            "application",
            "createdAtUtc",
            "database",
            "latestMigration",
            "postgresImage",
            "pgDumpVersion",
            "backupFile",
            "sizeBytes",
            "sha256",
        ]
        for key in required_manifest:
            if key not in manifest:
                fail(f"Manifest {dump.name} tidak memiliki properti {key}.")

        if manifest["schemaVersion"] != 1 or manifest["application"] != "SiPacul":
            fail(f"Identitas manifest tidak didukung untuk {dump.name}.")
        if str(manifest["backupFile"]) != dump.name:
            fail(f"Nama archive pada manifest tidak cocok untuk {dump.name}.")

        size = dump.stat().st_size
        try:
            manifest_size = int(manifest["sizeBytes"])
        except (TypeError, ValueError):
            fail(f"sizeBytes manifest tidak valid untuk {dump.name}.")
        if size <= 0 or manifest_size != size:
            fail(f"Ukuran archive tidak cocok untuk {dump.name}.")

        manifest_hash = str(manifest["sha256"]).upper()
        if not SHA_RE.fullmatch(manifest_hash):
            fail(f"SHA256 manifest tidak valid untuk {dump.name}.")

        try:
            checksum_line = checksum.read_text(encoding="utf-8").strip()
        except OSError as exc:
            fail(f"Sidecar SHA256 tidak dapat dibaca untuk {dump.name}: {exc}")
        checksum_match = SHA_LINE_RE.fullmatch(checksum_line)
        if checksum_match is None:
            fail(f"Format sidecar SHA256 tidak valid untuk {dump.name}.")
        side_hash = checksum_match.group(1).upper()
        side_name = checksum_match.group(2).strip()
        if side_name != dump.name or side_hash != manifest_hash:
            fail(f"Sidecar SHA256 tidak cocok untuk {dump.name}.")

        manifest_created = parse_iso_utc(manifest["createdAtUtc"], f"createdAtUtc {dump.name}")
        delay = manifest_created - created_utc
        if delay.total_seconds() < -300 or delay > dt.timedelta(days=7):
            fail(f"Urutan timestamp nama file dan manifest tidak wajar: {dump.name}.")

        results.append(
            BackupSet(
                dump=dump,
                checksum=checksum,
                manifest=manifest_path,
                created_utc=created_utc,
                manifest_created_utc=manifest_created,
                database=str(manifest["database"]),
                latest_migration=str(manifest["latestMigration"]),
                size_bytes=size,
                sha256=manifest_hash,
            )
        )

    return sorted(results, key=lambda item: item.created_utc, reverse=True)


def verify_hash(item: BackupSet) -> None:
    actual = sha256_file(item.dump)
    if actual != item.sha256:
        fail(f"SHA256 archive tidak cocok: {item.dump.name}")


def command_freshness(args: argparse.Namespace) -> int:
    if args.max_age_hours <= 0 or args.max_age_hours > 8760:
        fail("max-age-hours harus lebih dari 0 dan paling besar 8760.")
    if args.minimum_valid_backups < 1 or args.minimum_valid_backups > 10000:
        fail("minimum-valid-backups harus berada pada rentang 1-10000.")

    root = resolve_directory(args.directory)
    backups = scan_backup_sets(root)
    if len(backups) < args.minimum_valid_backups:
        fail(f"Backup valid hanya {len(backups)}; minimum {args.minimum_valid_backups}.")

    newest = backups[0]
    age_hours = (utc_now() - newest.created_utc).total_seconds() / 3600.0
    if age_hours < -(5.0 / 60.0):
        fail("Backup terbaru memiliki waktu lebih dari 5 menit di masa depan.")
    if age_hours > args.max_age_hours:
        fail(f"Backup terbaru berusia {age_hours:.2f} jam; batas {args.max_age_hours:.2f} jam.")

    if args.verify_all_hashes:
        for item in backups:
            verify_hash(item)
        scope = f"seluruh {len(backups)} archive"
    else:
        verify_hash(newest)
        scope = "archive terbaru"

    print(f"[OK] Folder backup: {root}")
    print(f"[OK] {len(backups)} backup valid; SHA256 {scope} cocok.")
    print(f"[OK] Backup terbaru berusia {max(0.0, age_hours):.2f} jam: {newest.dump.name}")
    print(f"[OK] Migration terbaru pada manifest: {newest.latest_migration}")
    print(f"[OK] Freshness berada di bawah batas {args.max_age_hours:.2f} jam.")
    return 0


def rollback_moves(moved: list[tuple[Path, Path]]) -> None:
    failures: list[str] = []
    for source, destination in reversed(moved):
        if destination.exists():
            try:
                os.replace(destination, source)
            except OSError:
                failures.append(str(destination))
    if failures:
        fail("Rollback retensi tidak lengkap; file tertinggal: " + ", ".join(failures))


def command_retention(args: argparse.Namespace) -> int:
    if args.retention_days < 0 or args.retention_days > 3650:
        fail("retention-days harus berada pada rentang 0-3650.")
    if args.minimum_backups < 1 or args.minimum_backups > 10000:
        fail("minimum-backups harus berada pada rentang 1-10000.")

    root = resolve_directory(args.directory)
    backups = scan_backup_sets(root)
    if not backups:
        fail("Tidak ada backup valid untuk dievaluasi.")

    cutoff = utc_now() - dt.timedelta(days=args.retention_days)
    candidates = [item for item in backups[args.minimum_backups:] if item.created_utc < cutoff]
    for item in candidates:
        verify_hash(item)

    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"[OK] Mode: {mode}; folder: {root}")
    print(f"[OK] {len(backups)} backup valid; {args.minimum_backups} backup terbaru selalu dilindungi.")
    print(f"[OK] {len(candidates)} kandidat melewati batas {args.retention_days} hari dan SHA256 kandidat valid.")

    if not args.apply:
        for item in candidates:
            print(f"[DRY-RUN] Akan menghapus triplet: {item.dump.name}")
        print("[OK] Dry-run selesai; tidak ada file yang diubah atau dihapus.")
        return 0

    trash = Path(tempfile.mkdtemp(prefix=".sipacul-retention-trash-", dir=root))
    moved: list[tuple[Path, Path]] = []
    try:
        for item in candidates:
            for source in (item.dump, item.checksum, item.manifest):
                destination = trash / source.name
                os.replace(source, destination)
                moved.append((source, destination))
    except Exception:
        rollback_moves(moved)
        shutil.rmtree(trash, ignore_errors=True)
        raise

    try:
        shutil.rmtree(trash)
    except OSError as exc:
        fail(f"Direktori transaksi retensi gagal dihapus: {exc}")

    remaining = scan_backup_sets(root)
    protected = min(args.minimum_backups, len(backups))
    if len(remaining) != len(backups) - len(candidates):
        fail("Jumlah backup setelah retensi tidak sesuai.")
    if len(remaining) < protected:
        fail("Retensi melanggar jumlah minimum backup.")

    for item in candidates:
        print(f"[HAPUS] Triplet kedaluwarsa: {item.dump.name}")
    print(f"[OK] {len(candidates)} backup kedaluwarsa dihapus; backup terbaru tetap dilindungi.")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="sipacul-backup-set.py")
    sub = parser.add_subparsers(dest="command", required=True)

    freshness = sub.add_parser("freshness")
    freshness.add_argument("--directory", required=True)
    freshness.add_argument("--max-age-hours", type=float, default=26.0)
    freshness.add_argument("--minimum-valid-backups", type=int, default=1)
    freshness.add_argument("--verify-all-hashes", action="store_true")
    freshness.set_defaults(handler=command_freshness)

    retention = sub.add_parser("retention")
    retention.add_argument("--directory", required=True)
    retention.add_argument("--retention-days", type=int, default=30)
    retention.add_argument("--minimum-backups", type=int, default=7)
    retention.add_argument("--apply", action="store_true")
    retention.set_defaults(handler=command_retention)

    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    try:
        return int(args.handler(args))
    except BackupError as exc:
        print(f"[GAGAL] {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
