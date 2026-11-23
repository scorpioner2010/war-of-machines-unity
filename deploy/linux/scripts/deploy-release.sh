#!/usr/bin/env bash
set -Eeuo pipefail
umask 027
if [[ $# -lt 2 || $# -gt 4 ]]; then
  echo "Usage: $0 <release-id> <release.tar.gz> [udp-port] [https-api-health-url]" >&2
  exit 2
fi
release_id="$1"
archive="$(realpath -- "$2")"
port=7770
if [[ $# -ge 3 ]]; then port="$3"; fi
api_health_url=""
if [[ $# -ge 4 ]]; then api_health_url="$4"; fi

if [[ ! "$release_id" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$ || "$release_id" == "." || "$release_id" == ".." ]]; then
  echo "Invalid release ID." >&2; exit 2
fi
if [[ ! -f "$archive" || ! -r "$archive" ]]; then
  echo "Release archive is not a readable regular file." >&2; exit 2
fi
if [[ ! "$port" =~ ^[0-9]{1,5}$ ]] || (( port < 1 || port > 65535 )); then
  echo "Invalid UDP port." >&2; exit 2
fi
if [[ -n "$api_health_url" && "$api_health_url" != https://* ]]; then
  echo "API health URL must use HTTPS." >&2; exit 2
fi

release_root="/opt/rav/releases"
current_link="/opt/rav/current"
service_name="rav-dedicated-server.service"
health_script="/usr/local/libexec/rav/health-check.sh"
install -d -o rav-server -g rav-server -m 0750 "$release_root"
release_dir="$release_root/$release_id"
stage_dir="$release_root/.staging-$release_id-$$"
next_link="/opt/rav/.current-$release_id-$$"
if [[ -e "$release_dir" || -L "$release_dir" || -e "$stage_dir" ]]; then
  echo "Release or staging path already exists; refusing to overwrite." >&2; exit 1
fi
if [[ -e "$current_link" && ! -L "$current_link" ]]; then
  echo "Current path exists and is not a symlink; refusing to replace it." >&2; exit 1
fi

while IFS= read -r member; do
  [[ "$member" != /* ]] || { echo "Archive contains an absolute path." >&2; exit 1; }
  [[ "$member" != *$'\r'* ]] || { echo "Archive contains an invalid path." >&2; exit 1; }
  IFS='/' read -r -a parts <<< "$member"
  for part in "${parts[@]}"; do
    [[ "$part" != ".." ]] || { echo "Archive path traversal was rejected." >&2; exit 1; }
  done
done < <(tar -tzf "$archive")
while IFS= read -r entry; do
  kind="$(printf '%s' "$entry" | cut -c1)"
  [[ "$kind" == "-" || "$kind" == "d" ]] || {
    echo "Archive contains a link or special file; refusing extraction." >&2; exit 1
  }
done < <(tar -tvzf "$archive")

mkdir -m 0750 "$stage_dir"
chown rav-server:rav-server "$stage_dir"
tar --extract --gzip --file "$archive" --directory "$stage_dir" --no-same-owner --no-same-permissions
executable="$stage_dir/RAVServer.x86_64"
if [[ ! -f "$executable" || ! -x "$executable" ]]; then
  echo "Archive does not contain an executable RAVServer.x86_64 at its root." >&2; exit 1
fi
if [[ ! -d "$stage_dir/RAVServer_Data" || ! -f "$stage_dir/RAVServer_Data/globalgamemanagers" || ! -f "$stage_dir/RAVServer_Data/Managed/Assembly-CSharp.dll" || ! -f "$stage_dir/RAVServer_Data/MonoBleedingEdge/x86_64/libmonobdwgc-2.0.so" || ! -f "$stage_dir/UnityPlayer.so" ]]; then
  echo "Release archive is missing required Unity data, Mono, or player runtime files." >&2; exit 1
fi
chown -R rav-server:rav-server "$stage_dir"
mv -- "$stage_dir" "$release_dir"

previous_target=""
if [[ -L "$current_link" ]]; then
  previous_target="$(readlink -f -- "$current_link")"
  if [[ -z "$previous_target" || "$previous_target" != "$release_root/"* || ! -d "$previous_target" ]]; then
    echo "Current symlink target is invalid; refusing deployment." >&2; exit 1
  fi
fi
ln -s "$release_dir" "$next_link"
mv -Tf -- "$next_link" "$current_link"
if systemctl restart "$service_name" && "$health_script" "$port" "$api_health_url"; then
  echo "Release $release_id is active."; exit 0
fi

echo "New release health check failed; restoring the previous release." >&2
if [[ -n "$previous_target" ]]; then
  ln -s "$previous_target" "$next_link"
  mv -Tf -- "$next_link" "$current_link"
  systemctl restart "$service_name" || true
else
  rm -f -- "$current_link"
  systemctl stop "$service_name" || true
fi
exit 1
