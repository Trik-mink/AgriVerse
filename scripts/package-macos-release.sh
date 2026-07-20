#!/bin/sh
set -eu

app_path="${1:-unity/Builds/Release/AgriVerse.app}"
archive_path="${2:-unity/Builds/Release/AgriVerse-macOS-Universal.zip}"
checksum_path="${archive_path}.sha256"
script_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

if [ -e "$archive_path" ] || [ -e "$checksum_path" ]; then
  echo "Archive destination already exists: $archive_path" >&2
  exit 1
fi

"$script_dir/verify-macos-release.sh" "$app_path"

ditto -c -k --sequesterRsrc --keepParent "$app_path" "$archive_path"
checksum="$(shasum -a 256 "$archive_path" | awk '{print $1}')"
printf '%s  %s\n' \
  "$checksum" \
  "$(basename -- "$archive_path")" > "$checksum_path"

extract_dir="$(mktemp -d "${TMPDIR:-/tmp}/agriverse-release.XXXXXX")"
trap 'rm -rf "$extract_dir"' EXIT HUP INT TERM
ditto -x -k "$archive_path" "$extract_dir"
"$script_dir/verify-macos-release.sh" "$extract_dir/AgriVerse.app"

uncompressed_bytes="$(du -sk "$app_path" | awk '{print $1 * 1024}')"
compressed_bytes="$(stat -f '%z' "$archive_path")"

printf 'Archive: %s\n' "$archive_path"
printf 'SHA-256: %s\n' "$checksum"
printf 'Uncompressed bytes: %s\n' "$uncompressed_bytes"
printf 'Compressed bytes: %s\n' "$compressed_bytes"
