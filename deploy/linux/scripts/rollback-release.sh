#!/usr/bin/env bash
set -Eeuo pipefail
umask 027
if [[ $# -lt 1 || $# -gt 3 ]]; then
  echo "Usage: $0 <existing-release-id> [udp-port] [https-api-health-url]" >&2
  exit 2
fi
release_id="$1"
port=7770
if [[ $# -ge 2 ]]; then port="$2"; fi
api_health_url=""
if [[ $# -ge 3 ]]; then api_health_url="$3"; fi
release_root="/opt/rav/releases"
release_dir="$release_root/$release_id"
current_link="/opt/rav/current"
next_link="/opt/rav/.rollback-$$"
service_name="rav-dedicated-server.service"
health_script="/usr/local/libexec/rav/health-check.sh"

if [[ ! "$release_id" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$ || "$release_id" == "." || "$release_id" == ".." ]]; then
  echo "Invalid release ID." >&2; exit 2
fi
if [[ ! -d "$release_dir" || ! -x "$release_dir/RAVServer.x86_64" ]]; then
  echo "Requested release does not exist or lacks the server executable." >&2; exit 1
fi
if [[ ! "$port" =~ ^[0-9]{1,5}$ ]] || (( port < 1 || port > 65535 )); then
  echo "Invalid UDP port." >&2; exit 2
fi
if [[ -n "$api_health_url" && "$api_health_url" != https://* ]]; then
  echo "API health URL must use HTTPS." >&2; exit 2
fi
if [[ -e "$current_link" && ! -L "$current_link" ]]; then
  echo "Current path exists and is not a symlink."; exit 1
fi
previous_target=""
if [[ -L "$current_link" ]]; then
  previous_target="$(readlink -f -- "$current_link")"
  if [[ -z "$previous_target" || "$previous_target" != "$release_root/"* || ! -d "$previous_target" ]]; then
    echo "Current symlink target is invalid."; exit 1
  fi
fi
ln -s "$release_dir" "$next_link"
mv -Tf -- "$next_link" "$current_link"
if systemctl restart "$service_name" && "$health_script" "$port" "$api_health_url"; then
  echo "Rolled back to release $release_id."; exit 0
fi
echo "Rollback health check failed; restoring the previous symlink." >&2
if [[ -n "$previous_target" ]]; then
  ln -s "$previous_target" "$next_link"
  mv -Tf -- "$next_link" "$current_link"
  systemctl restart "$service_name" || true
else
  rm -f -- "$current_link"
  systemctl stop "$service_name" || true
fi
exit 1
