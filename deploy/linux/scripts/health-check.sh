#!/usr/bin/env bash
set -Eeuo pipefail

service_name="rav-dedicated-server.service"
if [[ -v RAV_SYSTEMD_SERVICE && -n "$RAV_SYSTEMD_SERVICE" ]]; then service_name="$RAV_SYSTEMD_SERVICE"; fi
if [[ ( ! -v RAV_SYSTEMD_SERVICE || -z "$RAV_SYSTEMD_SERVICE" ) && -v WOM_SYSTEMD_SERVICE && -n "$WOM_SYSTEMD_SERVICE" ]]; then service_name="$WOM_SYSTEMD_SERVICE"; fi
port=7770
if [[ -v RAV_SERVER_PORT && -n "$RAV_SERVER_PORT" ]]; then port="$RAV_SERVER_PORT"; fi
if [[ ( ! -v RAV_SERVER_PORT || -z "$RAV_SERVER_PORT" ) && -v WOM_SERVER_PORT && -n "$WOM_SERVER_PORT" ]]; then port="$WOM_SERVER_PORT"; fi
if [[ $# -ge 1 ]]; then port="$1"; fi
api_health_url=""
if [[ -v RAV_API_HEALTH_URL ]]; then api_health_url="$RAV_API_HEALTH_URL"; fi
if [[ ( ! -v RAV_API_HEALTH_URL || -z "$RAV_API_HEALTH_URL" ) && -v WOM_API_HEALTH_URL ]]; then api_health_url="$WOM_API_HEALTH_URL"; fi
if [[ $# -ge 2 ]]; then api_health_url="$2"; fi

if [[ ! "$port" =~ ^[0-9]{1,5}$ ]] || (( port < 1 || port > 65535 )); then
  echo "Invalid UDP port." >&2
  exit 2
fi
systemctl is-active --quiet "$service_name" || {
  echo "Unity server service is not active." >&2
  exit 1
}
if ! ss -H -lun "sport = :$port" | grep -q .; then
  echo "No UDP socket is listening on port $port." >&2
  exit 1
fi
if [[ -n "$api_health_url" ]]; then
  case "$api_health_url" in
    https://*) ;;
    *) echo "API health URL must use HTTPS." >&2; exit 2 ;;
  esac
  curl --fail --silent --show-error --max-time 8 "$api_health_url" >/dev/null
fi
echo "Unity service is active and UDP port $port is listening."
