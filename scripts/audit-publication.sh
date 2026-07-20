#!/bin/sh
set -eu

repository_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
cd "$repository_root"

findings_file="$(mktemp "${TMPDIR:-/tmp}/agriverse-publication-audit.XXXXXX")"
trap 'rm -f "$findings_file"' EXIT HUP INT TERM
audit_failed=0

report_findings() {
  label="$1"
  if [ -s "$findings_file" ]; then
    count="$(wc -l < "$findings_file" | tr -d ' ')"
    printf '%s: FAIL (%s path or commit findings; values suppressed)\n' \
      "$label" "$count"
    sed -n '1,40p' "$findings_file"
    audit_failed=1
  else
    printf '%s: PASS\n' "$label"
  fi
  : > "$findings_file"
}

credential_pattern='sk-[A-Za-z0-9_-]{20,}|ghp_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|rnd_[A-Za-z0-9_-]{20,}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----'
privacy_pattern='/Users/tristan|/Users/[^/]+/Downloads|[A-Za-z0-9._%+-]+@gmail[.]com'

git grep -I -l -E "$credential_pattern" -- . \
  2>/dev/null |
grep -v '^scripts/audit-publication[.]sh$' \
  > "$findings_file" || true
report_findings "Tracked credential-shape scan"

git grep -I -l -E "$privacy_pattern" -- . \
  2>/dev/null |
grep -v '^scripts/audit-publication[.]sh$' \
  > "$findings_file" || true
report_findings "Tracked private-metadata scan"

git ls-files --others --exclude-standard -z |
xargs -0 rg -I -l --no-messages \
  -e "$credential_pattern" \
  -e "$privacy_pattern" \
  2>/dev/null |
grep -v '^scripts/audit-publication[.]sh$' \
  > "$findings_file" || true
report_findings "Untracked publication-candidate scan"

git rev-list --all |
while IFS= read -r commit; do
  git grep -I -l -E "$credential_pattern" "$commit" -- 2>/dev/null |
    sed "s#^#$commit:#" || true
done |
sort -u |
grep -v ':scripts/audit-publication[.]sh$' \
  > "$findings_file" || true
report_findings "Reachable-history credential-shape scan"

git rev-list --all |
while IFS= read -r commit; do
  git grep -I -l -E "$privacy_pattern" "$commit" -- 2>/dev/null |
    sed "s#^#$commit:#" || true
done |
sort -u |
grep -v ':scripts/audit-publication[.]sh$' \
  > "$findings_file" || true
report_findings "Reachable-history private-metadata scan"

git log --all --format='%H %ae %ce' |
awk 'tolower($0) ~ /@gmail[.]com/ { print $1 " metadata-email" }' \
  > "$findings_file"
report_findings "Reachable Git metadata scan"

git rev-list --objects --all |
awk '
  NF > 1 {
    path = substr($0, index($0, " ") + 1)
    if (path == ".env" ||
        (path ~ /^\.env\./ && path != ".env.example") ||
        path == "docs\/BRAIN-HANDOFF.md") {
      print $1 " " path
    }
  }
' > "$findings_file"
report_findings "Private-path history scan"

if [ -d .git/lfs/objects ]; then
  rg -a -l --no-messages \
    -e "$credential_pattern" \
    -e "$privacy_pattern" \
    .git/lfs/objects > "$findings_file" || true
fi
report_findings "Local Git LFS object-content scan"

if git ls-files --error-unmatch docs/BRAIN-HANDOFF.md >/dev/null 2>&1; then
  printf '%s\n' 'docs/BRAIN-HANDOFF.md is tracked' > "$findings_file"
fi
if ! git check-ignore -q docs/BRAIN-HANDOFF.md; then
  printf '%s\n' 'docs/BRAIN-HANDOFF.md is not locally ignored' >> "$findings_file"
fi
report_findings "Private handoff exclusion"

if [ "$audit_failed" -ne 0 ]; then
  printf '%s\n' "Publication audit failed."
  exit 1
fi

printf '%s\n' "Publication audit passed; no secret values were printed."
