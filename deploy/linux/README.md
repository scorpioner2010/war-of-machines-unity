# Linux Dedicated Server deployment templates

These files prepare a future Ubuntu 24.04 staging host. They were not installed or run against a server during Stage 2.

## Provisioning outline

1. Create a dedicated rav-server system user/group with no interactive login. Create /opt/rav/releases, /var/lib/rav, /var/log/rav, /etc/rav/credentials, and /usr/local/libexec/rav with least-privilege ownership.
2. Copy the systemd unit to /etc/systemd/system/rav-dedicated-server.service. Install health-check.sh to /usr/local/libexec/rav/health-check.sh; install release scripts under an administrator-owned directory. Make scripts executable and review them on the target host.
3. Copy rav-server.env.example to /etc/rav/rav-server.env, fill in values from the approved staging provisioning process, set owner root:rav-server and mode 0640. Keep credentials out of Git and shell history.
4. Place DTLS certificate, private key, and CA certificate at the paths referenced by LoadCredential; owner root and mode 0640 is sufficient because systemd opens them before dropping service privileges. Never put private keys in the release archive or environment template.
5. Install rav-dedicated-server under /etc/logrotate.d/rav-dedicated-server, create the log directory owned by rav-server, allow outbound HTTPS to the API and inbound UDP on the configured RAV_SERVER_PORT (default 7770). Do not open PostgreSQL publicly.
6. Build/package a Linux server release, copy its archive to the host over an authenticated channel, and run deploy-release.sh <release-id> <archive> [udp-port] [https-api-health-url] as root. The optional health URL must use HTTPS; curl keeps standard CA/TLS verification enabled.
7. Confirm systemctl status, journalctl, UDP listener, API readiness, DTLS/client handshake, heartbeat, and match start/end in isolated staging before public traffic.

deploy-release.sh creates a fresh versioned release directory and switches /opt/rav/current with an atomic symlink replacement. Failed health checks restore the previous symlink. rollback-release.sh selects an existing release and runs the same checks. Neither script deletes old releases. Keep a written release inventory and remove old releases only through a separate reviewed retention operation.

## Log handling and recovery

The unit sends RAV dedicated-server stdout to /var/log/rav/rav-server.log; logrotate retains 14 daily rotations with copytruncate. Verify disk headroom and logrotate on staging because Unity may emit bursts during crashes or matchmaking errors. systemd/journald remains available for unit events.

Systemd restarts the server on process failure and sends SIGTERM before the 45-second stop timeout. The service uses a dedicated identity, strict filesystem protection, private temporary storage, systemd credentials, and write access only to state/log directories. Review those limits against Unity behavior during staging; do not weaken DTLS or certificate verification to make a test pass.

The API repository owns PostgreSQL backup/restore scripts in deploy/production/scripts/. Use them only after provisioning isolated staging secrets and a confirmed empty restore database. A custom-format pg_dump archive supports pg_restore --list validation. Restore refuses a non-empty public schema and does not drop objects or run EF migrations. Store encrypted off-host copies and rehearse restore before production planning.
