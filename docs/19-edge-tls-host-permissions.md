# Edge TLS host permissions

## Purpose

This contract prevents a host UID collision from weakening the SiPacul TLS private-key boundary.

The edge image runs as UID 101. On the production Ubuntu host, UID/GID 101 may
already belong to an unrelated system account. Host TLS material MUST NOT be
chowned to numeric UID or GID 101 merely to make the bind mount readable.

## Runtime identity

The production Compose edge service explicitly runs as:

    user: "101:0"

This preserves the unprivileged edge UID while assigning container GID 0.

The edge image remains non-root because its effective UID is 101. GID 0 is used
only so a root-owned, group-readable TLS private key can be read through the
read-only bind mount.

## Host TLS ownership

Private-staging and later public TLS material use these host permissions:

    certificate: root:root 0644
    private key: root:root 0640

The private key MUST NOT be:

- owned by host UID 101;
- group-owned by host GID 101;
- world-readable;
- stored in Git.

The directory remains outside the repository at `/etc/sipacul/tls`.

## Production environment

`/etc/sipacul/.env.production` remains outside Git and should be root-owned with
mode 0600.

Public activation remains disabled during private staging:

    SIPACUL_PUBLIC_ACTIVATION=disabled
    SIPACUL_PUBLIC_HOSTNAME=_
    SIPACUL_HSTS_ENABLED=false
    SIPACUL_BIND_ADDRESS=127.0.0.1
    SIPACUL_HTTPS_PORT=8443

Changing the TLS readability contract does not authorize DNS, firewall, public
443, certificate issuance, HSTS, database migration, or deployment.

## Validation boundary

Before any TLS material is provisioned on a host:

1. verify the immutable production checkpoint;
2. verify the edge Compose service contains `user: "101:0"`;
3. verify the host TLS directory contains no pre-existing material;
4. provision the private key as `root:root 0640`;
5. validate Compose configuration before any container starts.

This contract intentionally avoids relying on the host meaning of numeric UID
101.
