#!/usr/bin/env bash
# Cross-platform functional verification of the guardian-program-station CLI.
# Runs the real binary on Windows (Git Bash), Linux, and macOS.
#
# It exercises the actual CLI processes end to end: help/version, create
# (with real filesystem verification), preview (node names + UTF-8), validate
# (valid + invalid), template (create/list/export/import/delete round-trip),
# and the exit-code contract (0 success / 2 invalid args / 3 validation /
# 4 operation failed).
#
# All data is isolated in a temp directory via GUARDIAN_PROGRAM_STATION_DATA.
# No user data is touched.
#
# Usage: verify-cli.sh <cli-binary-path>

set -uo pipefail

CLI="$1"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TREE_DIR="$SCRIPT_DIR/test-trees"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

WORK="$(mktemp -d)"
DATA="$WORK/data"
OUT="$WORK/out"
IMPORT_SRC="$WORK/import-src"
mkdir -p "$DATA" "$OUT" "$IMPORT_SRC/Sub/Deep"

export GUARDIAN_PROGRAM_STATION_DATA="$DATA"

LOG="$REPO_ROOT/cli-verify.log"
: > "$LOG"

PASS=0
FAIL=0
FAILURES=()

note() { printf '%s\n' "$*" | tee -a "$LOG"; }

ok()   { PASS=$((PASS + 1)); note "PASS: $*"; }
bad()  { FAIL=$((FAIL + 1)); FAILURES+=("$*"); note "FAIL: $*"; }

# run_cli <out-var> <code-var> <args...>
run_cli() {
  local __out="$1" __code="$2"
  shift 2
  local __o __c
  __o="$("$CLI" "$@" 2>"$WORK/err.txt")"
  __c=$?
  eval "$__out=\"\$__o\""
  eval "$__code=\$__c"
}

# expect_exit <desc> <expected-code> <args...>
expect_exit() {
  local desc="$1" expected="$2"
  shift 2
  local out code
  run_cli out code "$@"
  if [ "$code" -eq "$expected" ]; then
    ok "$desc (exit $code)"
  else
    bad "$desc (expected exit $expected, got $code)"
    note "  stdout: $(printf '%s' "$out" | tr '\r\n' '  ' | head -c 300)"
  fi
}

# expect_exit_output <desc> <expected-code> <pattern> <args...>
expect_exit_output() {
  local desc="$1" expected="$2" pattern="$3"
  shift 3
  local out code
  run_cli out code "$@"
  if [ "$code" -ne "$expected" ]; then
    bad "$desc (expected exit $expected, got $code)"
    return
  fi
  if printf '%s' "$out" | grep -qF -- "$pattern"; then
    ok "$desc (exit $code, contains '$pattern')"
  else
    bad "$desc (exit $code OK but stdout lacks '$pattern')"
    note "  stdout: $(printf '%s' "$out" | tr '\r\n' '  ' | head -c 300)"
  fi
}

note "=== guardian-program-station CLI verification ==="
note "CLI: $CLI"
note "OS: $(uname -s)  ($(uname -m))"
note "Isolated data dir: $DATA"
note ""

# ---- Help / Version ----
note "--- help / version ---"
expect_exit_output "help returns usage with all commands" 0 "create" --help
expect_exit_output "help lists preview" 0 "preview" --help
expect_exit_output "help lists validate" 0 "validate" --help
expect_exit_output "help lists template" 0 "template" --help
expect_exit_output "create --help shows --tree option" 0 "--tree" create --help
expect_exit_output "version prints unified version" 0 "1.0.0" --version

# ---- Exit codes ----
note "--- exit codes ---"
expect_exit "unknown command -> 2" 2 frobnicate
expect_exit "create missing required args -> 2" 2 create
expect_exit "preview missing --tree -> 2" 2 preview
expect_exit "validate missing --tree -> 2" 2 validate
expect_exit "template delete missing --id -> 2" 2 template delete
expect_exit "validate invalid tree -> 3" 3 validate --tree "$TREE_DIR/invalid.json"
expect_exit "preview nonexistent tree file -> 4" 4 preview --tree "$WORK/missing.json"
expect_exit "template create nonexistent file -> 4" 4 template create --tree "$WORK/missing.json"

# ---- Create (real filesystem verification) ----
note "--- create ---"
expect_exit "create valid tree -> 0" 0 create --tree "$TREE_DIR/valid.json" --path "$OUT/created"
if [ -d "$OUT/created/Project/Source/Core" ] \
  && [ -d "$OUT/created/Project/Source/UI" ] \
  && [ -d "$OUT/created/Project/Tests" ]; then
  ok "create produced Project/Source/Core, UI, Tests folders"
else
  bad "create did not produce the expected folder structure"
  find "$OUT/created" -maxdepth 3 2>/dev/null | head -20 >> "$LOG"
fi

expect_exit "create file node -> 0" 0 create --tree "$TREE_DIR/file-node.json" --path "$OUT/files"
if [ -f "$OUT/files/Docs/readme.txt" ]; then
  ok "create produced the file node Docs/readme.txt"
else
  bad "create did not produce the file node Docs/readme.txt"
fi

# create must not delete existing files
mkdir -p "$OUT/keep/Project"
printf 'keep' > "$OUT/keep/Project/keep.txt"
expect_exit "create over existing tree -> 0" 0 create --tree "$TREE_DIR/valid.json" --path "$OUT/keep"
if [ -f "$OUT/keep/Project/keep.txt" ] && [ "$(cat "$OUT/keep/Project/keep.txt")" = "keep" ]; then
  ok "create preserved existing files (no overwrite)"
else
  bad "create overwrote or removed an existing file"
fi

# ---- Preview ----
note "--- preview ---"
expect_exit_output "preview valid tree contains root node" 0 "Project" preview --tree "$TREE_DIR/valid.json"
expect_exit_output "preview contains nested node Core" 0 "Core" preview --tree "$TREE_DIR/valid.json"
expect_exit_output "preview contains Tests" 0 "Tests" preview --tree "$TREE_DIR/valid.json"
expect_exit_output "preview draws box connectors" 0 "├──" preview --tree "$TREE_DIR/valid.json"
expect_exit_output "preview draws last-child connector" 0 "└──" preview --tree "$TREE_DIR/valid.json"

# ---- Validate ----
note "--- validate ---"
run_cli vout vcode validate --tree "$TREE_DIR/valid.json"
if [ "$vcode" -eq 0 ] && [ "$(printf '%s' "$vout" | tr -d '\r\n')" = "Valid" ]; then
  ok "validate valid tree prints 'Valid' (exit 0)"
else
  bad "validate valid tree (exit $vcode, output '$(printf '%s' "$vout" | tr '\r\n' '  ')')"
fi

run_cli ivout ivcode validate --tree "$TREE_DIR/invalid.json"
if [ "$ivcode" -eq 3 ] && [ "$(printf '%s' "$ivout" | tr -d '\r\n')" = "Invalid" ]; then
  ok "validate invalid tree prints 'Invalid' (exit 3)"
else
  bad "validate invalid tree (exit $ivcode, output '$(printf '%s' "$ivout" | tr '\r\n' '  ')')"
fi

# ---- Template (isolated data dir) ----
note "--- template ---"
expect_exit "template create -> 0" 0 template create --tree "$TREE_DIR/valid.json" --name "CiTemplate"
expect_exit_output "template list shows the new template" 0 "CiTemplate" template list
expect_exit "template export -> 0" 0 template export --id cli-test-1 --output "$WORK/exported.json"
if [ -f "$WORK/exported.json" ]; then
  ok "template export wrote the JSON file"
else
  bad "template export did not write the JSON file"
fi
expect_exit "template import -> 0" 0 template import --path "$IMPORT_SRC"
expect_exit "template delete -> 0" 0 template delete --id cli-test-1
run_cli lout lcode template list
if [ "$lcode" -eq 0 ] && ! printf '%s' "$lout" | grep -qF -- "CliTemplate"; then
  ok "template delete removed the template from list"
else
  bad "template delete left the template in the list"
fi

# ---- UTF-8 (Arabic / Chinese / Japanese) ----
note "--- UTF-8 ---"
expect_exit "create utf8 tree -> 0" 0 create --tree "$TREE_DIR/utf8.json" --path "$OUT/utf8"
if [ -d "$OUT/utf8/اختبار عربي" ] && [ -d "$OUT/utf8/测试" ] && [ -d "$OUT/utf8/テスト/サブ" ]; then
  ok "utf8 create produced Arabic/Chinese/Japanese folders"
else
  bad "utf8 create missing one of the Unicode folders"
  find "$OUT/utf8" -maxdepth 3 2>/dev/null | head -20 >> "$LOG"
fi

run_cli uout ucode preview --tree "$TREE_DIR/utf8.json"
REPLACEMENT_CHAR=$(printf '\xef\xbf\xbd')
if [ "$ucode" -eq 0 ] \
  && printf '%s' "$uout" | grep -qF -- "اختبار عربي" \
  && printf '%s' "$uout" | grep -qF -- "测试" \
  && printf '%s' "$uout" | grep -qF -- "テスト" \
  && ! printf '%s' "$uout" | grep -qF -- "$REPLACEMENT_CHAR"; then
  ok "utf8 preview keeps all names intact, no replacement characters"
else
  bad "utf8 preview corrupted text or produced replacement characters"
  note "  stdout: $(printf '%s' "$uout" | tr '\r\n' '  ' | head -c 300)"
fi

# ---- Summary ----
note ""
note "=========================================="
note "RESULT: $PASS passed, $FAIL failed"
if [ "$FAIL" -gt 0 ]; then
  for f in "${FAILURES[@]}"; do
    note "  - $f"
  done
fi
note "Log: $LOG"

rm -rf "$WORK"

if [ "$FAIL" -gt 0 ]; then
  exit 1
fi
exit 0
