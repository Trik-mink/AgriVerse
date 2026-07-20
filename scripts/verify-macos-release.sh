#!/bin/sh
set -eu

app_path="${1:-unity/Builds/Release/AgriVerse.app}"

if [ ! -d "$app_path" ]; then
  echo "Release app not found: $app_path" >&2
  exit 1
fi

plist="$app_path/Contents/Info.plist"
macos_dir="$app_path/Contents/MacOS"
data_dir="$app_path/Contents/Resources/Data"

executable_name="$(/usr/libexec/PlistBuddy \
  -c 'Print :CFBundleExecutable' "$plist")"
bundle_name="$(/usr/libexec/PlistBuddy \
  -c 'Print :CFBundleName' "$plist")"
bundle_identifier="$(/usr/libexec/PlistBuddy \
  -c 'Print :CFBundleIdentifier' "$plist")"

[ "$executable_name" = "AgriVerse" ]
[ "$bundle_name" = "AgriVerse" ]
[ "$bundle_identifier" = "org.agriverse.episode1" ]

executable_count="$(find "$macos_dir" -type f -perm -111 | wc -l | tr -d ' ')"
[ "$executable_count" = "1" ]
[ -x "$macos_dir/AgriVerse" ]

architectures="$(lipo -archs "$macos_dir/AgriVerse")"
case " $architectures " in
  *" arm64 "*) ;;
  *) echo "arm64 slice is missing: $architectures" >&2; exit 1 ;;
esac
case " $architectures " in
  *" x86_64 "*) ;;
  *) echo "x86_64 slice is missing: $architectures" >&2; exit 1 ;;
esac

codesign --verify --deep --strict "$app_path"

scene_manifest="$data_dir/globalgamemanagers"
if ! strings "$scene_manifest" | grep -q 'Assets/Scenes/Episode3DAlpha.unity'; then
  echo "Episode3DAlpha release scene is missing." >&2
  exit 1
fi
if strings "$scene_manifest" | grep -q 'Assets/Scenes/SampleScene.unity'; then
  echo "SampleScene was included in the release." >&2
  exit 1
fi

if find "$app_path" -type f \( -name '.env' -o -name '.env.*' \) |
  grep -q .; then
  echo "An environment file was bundled into the app." >&2
  exit 1
fi

if LC_ALL=C grep -aE -r -q \
  'sk-[A-Za-z0-9_-]{20,}|ghp_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}' \
  "$app_path"; then
  echo "A credential-shaped value was found in the app." >&2
  exit 1
fi

printf 'Bundle name: %s\n' "$bundle_name"
printf 'Bundle identifier: %s\n' "$bundle_identifier"
printf 'Executable: %s (exactly one)\n' "$executable_name"
printf 'Architectures: %s\n' "$architectures"
printf 'Release scene: Assets/Scenes/Episode3DAlpha.unity\n'
printf 'Code signature: valid\n'
printf 'Credential-shaped values: none found\n'
