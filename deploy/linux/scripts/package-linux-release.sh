#!/usr/bin/env bash
set -Eeuo pipefail
umask 077
if [[ $# -ne 3 ]]; then
  echo "Usage: $0 <linux-build-directory> <release-id> <output-directory>" >&2
  exit 2
fi
source_dir="$(realpath -- "$1")"
release_id="$2"
output_dir="$(realpath -m -- "$3")"
if [[ ! "$release_id" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$ || "$release_id" == "." || "$release_id" == ".." ]]; then
  echo "Invalid release ID." >&2
  exit 2
fi
if [[ ! -d "$source_dir" || ! -f "$source_dir/RAVServer.x86_64" ]]; then
  echo "Build directory is missing RAVServer.x86_64." >&2
  exit 1
fi
if [[ ! -d "$source_dir/RAVServer_Data" || ! -f "$source_dir/RAVServer_Data/globalgamemanagers" || ! -f "$source_dir/RAVServer_Data/Managed/Assembly-CSharp.dll" || ! -f "$source_dir/RAVServer_Data/MonoBleedingEdge/x86_64/libmonobdwgc-2.0.so" || ! -f "$source_dir/UnityPlayer.so" ]]; then
  echo "Build directory is missing required Unity data, Mono, or player runtime files." >&2
  exit 1
fi
install -d -m 0700 "$output_dir"
case "$output_dir/" in
  "$source_dir/"*) echo "Output directory must be outside the build directory." >&2; exit 2 ;;
esac
archive="$output_dir/RAVServer-$release_id.tar.gz"
if [[ -e "$archive" || -L "$archive" ]]; then
  echo "Archive already exists; refusing to overwrite." >&2
  exit 1
fi
staging="$(mktemp -d "$output_dir/.rav-package-$release_id.XXXXXX")"
temporary="$(mktemp "$output_dir/.RAVServer-$release_id.XXXXXX")"
trap 'rm -rf -- "$staging"; rm -f -- "$temporary"' EXIT
cp -a "$source_dir/." "$staging/"
chmod 0755 "$staging/RAVServer.x86_64"
tar --create --gzip --file "$temporary" --directory "$staging" --exclude=./RAV_BurstDebugInformation_DoNotShip .
mv -- "$temporary" "$archive"
chmod 0600 "$archive"
rm -rf -- "$staging"
trap - EXIT
echo "$archive"
