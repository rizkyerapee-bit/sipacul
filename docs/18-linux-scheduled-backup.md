# Sprint 20D2G3C6 - Linux Scheduled PostgreSQL Backup

Sprint 20D2G3C6 extends the Linux production operations bridge with a scheduled
PostgreSQL backup lifecycle for Ubuntu production hosts.

The scope preserves the existing SiPacul backup contract: custom-format archive,
SHA256 sidecar, JSON manifest, freshness checks, retention with a protected
minimum, exclusive execution, and operational logging. It does not deploy the
application, publish images, change DNS/firewall/certificates, or expose a public
port.

## Artifacts

```text
operations/linux/backup-postgres.sh                 (modified: backup operation lock)
operations/linux/sipacul-backup-set.py
operations/linux/backup-retention.sh
operations/linux/backup-freshness.sh
operations/linux/backup-cycle.sh
operations/linux/install-backup-systemd.sh
operations/linux/systemd/sipacul-postgres-backup.service
operations/linux/systemd/sipacul-postgres-backup.timer
operations/Test-SiPaculLinuxScheduledBackup-PS51.ps1
docs/18-linux-scheduled-backup.md
```

## Default production paths

```text
Repository:       /opt/sipacul/repository
Environment:      /etc/sipacul/.env.production
Backup directory: /var/backups/sipacul
Cycle log:        /var/log/sipacul/backup-cycle.log
Operation lock:   /run/lock/sipacul-postgres-backup-operation.lock
Cycle lock:       /run/lock/sipacul-postgres-backup-cycle.lock
```

The systemd service runs as root so `sipaculadmin` does not need membership in
the Docker group.

## Backup operation lock

`backup-postgres.sh` now obtains an exclusive non-blocking `flock` before it
inspects PostgreSQL or creates an archive.

This lock applies to every Linux backup invocation, including:

- scheduled backup;
- pre-deploy backup;
- pre-rollback backup;
- operator-triggered manual backup.

A second backup attempt fails closed while another backup operation owns the
lock. No container, volume, or existing backup is removed because of lock
contention.

## Backup set validation

`sipacul-backup-set.py` is the canonical Linux reader for backup triplets:

```text
sipacul-postgres-<UTC timestamp>.dump
sipacul-postgres-<UTC timestamp>.dump.sha256
sipacul-postgres-<UTC timestamp>.dump.json
```

It validates:

- archive naming and UTC timestamp;
- manifest schemaVersion and application identity;
- required manifest properties;
- archive name and size consistency;
- SHA256 format and sidecar agreement;
- manifest timestamp sanity;
- actual archive SHA256 when integrity verification is requested.

Short-lived incomplete finalization artifacts are tolerated for five minutes so
an audit cannot falsely fail during another backup's atomic finalization window.
Stale incomplete backup sets still fail validation.

## Retention

Default retention policy:

```text
RetentionDays  = 30
MinimumBackups = 7
```

`backup-retention.sh` is dry-run by default. `--apply` is required for deletion.

Only backups that are both:

1. outside the newest `MinimumBackups`; and
2. older than `RetentionDays`

are candidates.

Candidate archive hashes are verified before deletion. Apply mode first moves
all candidate triplet files into an isolated transaction directory inside the
backup filesystem. Move failure triggers rollback. The transaction directory is
removed only after every move succeeds.

## Freshness

Default freshness policy:

```text
MaxAgeHours = 26
MinimumValidBackups = 1
```

The newest archive hash is always verified. `--verify-all-hashes` verifies every
recognized backup set.

Freshness fails if:

- there are fewer than the requested valid backups;
- the newest backup is older than the maximum age;
- a backup timestamp is materially in the future;
- the required SHA256 integrity check fails.

## Scheduled backup cycle

`backup-cycle.sh` runs the stages in this order:

1. acquire an exclusive cycle lock;
2. validate repository/environment and clean Git state;
3. create one PostgreSQL backup;
4. evaluate/apply retention;
5. verify freshness and integrity;
6. verify Git HEAD and working tree are unchanged;
7. write the cycle result to the operational log.

Manual cycle execution defaults to retention dry-run. Production systemd uses
`--apply-retention --verify-all-hashes` explicitly.

## systemd service and timer

The service executes:

```text
/opt/sipacul/repository/operations/linux/backup-cycle.sh
```

with production paths, 30-day retention, minimum seven backups, 26-hour
freshness, retention apply, and full hash verification.

The timer schedule is:

```text
02:00 Asia/Jakarta every day
Persistent=true
```

The VPS can remain on UTC. systemd interprets the schedule using the explicit
Asia/Jakarta timezone. `Persistent=true` allows a missed run to be triggered
after the host becomes available again.

The service has a four-hour start timeout and lowers CPU/I/O scheduling priority
to reduce impact on the application.

## Installation lifecycle

`install-backup-systemd.sh` is plan-only by default.

Plan:

```bash
sudo ./operations/linux/install-backup-systemd.sh
```

Install units but leave the timer activation state unchanged:

```bash
sudo ./operations/linux/install-backup-systemd.sh --execute
```

Initial production activation should happen only after the private initial
SiPacul deployment is healthy and a manual backup cycle has passed:

```bash
sudo ./operations/linux/install-backup-systemd.sh --execute --enable
```

If existing unit files differ, the installer fails unless `--force` is given
after operator review. Identical units are idempotent.

## Production activation sequence

The intended host sequence is:

1. place the release repository at `/opt/sipacul/repository`;
2. create `/etc/sipacul/.env.production` and TLS files through the separate
   secret/certificate procedure;
3. perform private initial deployment;
4. run one manual backup cycle and freshness check;
5. install systemd units with `--execute`;
6. inspect unit/timer state;
7. enable the timer with an explicit operation;
8. verify the next scheduled time and last backup freshness.

The timer must not be enabled before PostgreSQL exists as a managed healthy
SiPacul container.

## Failure safety

This scope does not:

- execute `docker compose down --volumes`;
- delete PostgreSQL or Data Protection volumes;
- run production `pg_restore`;
- downgrade schema;
- modify `.env.production`;
- store GHCR credentials;
- deploy application images;
- change DNS;
- change UFW or provider firewall;
- create or replace certificates;
- open port 443;
- enable HSTS.

## Acceptance

Sprint 20D2G3C6 is ready when:

1. every Linux backup operation uses an exclusive lock;
2. retention is dry-run by default and protects the newest minimum backup sets;
3. deletion candidates are hash-verified before apply;
4. retention uses a same-filesystem transaction directory with rollback on move
   failure;
5. freshness validates backup count, age, and SHA256 integrity;
6. scheduled cycle runs backup -> retention -> freshness in that order;
7. cycle has a separate non-overlap lock and log;
8. systemd service uses root without adding `sipaculadmin` to the Docker group;
9. timer uses 02:00 Asia/Jakarta and `Persistent=true`;
10. systemd installer is plan-only by default and activation is explicit;
11. no public/deployment/database-restore mutation is introduced;
12. Windows production backup lifecycle remains unchanged.
