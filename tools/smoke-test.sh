#!/usr/bin/env bash
# FluentPDF Comprehensive Smoke Test
# Usage: bash tools/smoke-test.sh [pdf_path]
# Requires: app running with --api-server on port 5000
#
# Two modes:
#   1. Server-side (fast, reliable): POST /api/gui/smoke-test
#   2. Client-side (verbose, per-endpoint): individual curl calls
#
# Default: server-side. Use --verbose for client-side.

set -euo pipefail
BASE="http://localhost:5000"
PDF="${1:-C:/Users/ryosu/Downloads/Auction_Invitation_Fixed_Final.pdf}"
MODE="${2:-fast}"
PASS=0; FAIL=0

# Check app is running
if ! curl -s --max-time 5 "$BASE/api/health" | grep -q "healthy"; then
  echo "ERROR: App not running at $BASE. Start with: FluentPDF.Avalonia.exe --api-server"
  exit 1
fi

if [ "$MODE" = "--verbose" ]; then
  # --- Client-side verbose mode ---
  step() {
    local name="$1"; shift
    local result
    result=$(curl -s --max-time 20 "$@" 2>&1) || true
    if echo "$result" | python -c "import sys,json; d=json.load(sys.stdin); exit(0 if d.get('success',d.get('sent',d.get('status','')))in[True,'healthy'] else 1)" 2>/dev/null; then
      printf "  %-40s PASS\n" "$name"; PASS=$((PASS+1))
    else
      printf "  %-40s FAIL  %s\n" "$name" "$(echo "$result" | head -c 100)"; FAIL=$((FAIL+1))
    fi
    sleep 1
  }

  echo "============================================"
  echo "  FluentPDF Smoke Test (verbose)"
  echo "============================================"
  echo ""

  echo "[Core]"
  step "health" "$BASE/api/health"
  step "open_pdf" -X POST "$BASE/api/gui/open" -H "Content-Type: application/json" -d "{\"filePath\":\"$PDF\"}"
  sleep 2

  echo "[Navigation]"
  step "navigate_page2" -X POST "$BASE/api/gui/navigate" -H "Content-Type: application/json" -d '{"page":2}'
  step "navigate_page1" -X POST "$BASE/api/gui/navigate" -H "Content-Type: application/json" -d '{"page":1}'

  echo "[Zoom]"
  step "zoom_150" -X POST "$BASE/api/gui/zoom" -H "Content-Type: application/json" -d '{"level":1.5}'
  step "zoom_100" -X POST "$BASE/api/gui/zoom" -H "Content-Type: application/json" -d '{"level":1.0}'

  echo "[Panels]"
  for p in thumbnails bookmarks search annotations metadata; do
    step "panel_$p" -X POST "$BASE/api/gui/panel" -H "Content-Type: application/json" -d "{\"panel\":\"$p\"}"
    step "panel_${p}_restore" -X POST "$BASE/api/gui/panel" -H "Content-Type: application/json" -d "{\"panel\":\"$p\"}"
  done

  echo "[Page Info]"
  step "page_info" "$BASE/api/gui/page-info"

  echo "[Search]"
  step "search" -X POST "$BASE/api/gui/search" -H "Content-Type: application/json" -d '{"query":"NFT"}'

  echo "[Text]"
  step "extract_text" -X POST "$BASE/api/gui/extract-text" -H "Content-Type: application/json" -d '{"pageNumber":1}'

  echo "[Edit Mode]"
  step "edit_on" -X POST "$BASE/api/gui/edit-mode" -H "Content-Type: application/json" -d '{"enabled":true}'
  step "edit_off" -X POST "$BASE/api/gui/edit-mode" -H "Content-Type: application/json" -d '{"enabled":false}'

  echo "[Bookmarks]"
  step "bookmarks_list" "$BASE/api/gui/user-bookmarks"
  step "bookmark_toggle" -X POST "$BASE/api/gui/user-bookmarks/toggle"
  step "bookmark_restore" -X POST "$BASE/api/gui/user-bookmarks/toggle"

  echo "[Notifications]"
  step "toast_info" -X POST "$BASE/api/gui/notify" -H "Content-Type: application/json" -d '{"message":"Test","level":"info"}'

  echo "[Cleanup]"
  step "close_tab" -X POST "$BASE/api/gui/close-tab"
  step "health_final" "$BASE/api/health"

  echo ""
  echo "============================================"
  printf "  Results: %d passed, %d failed\n" "$PASS" "$FAIL"
  echo "============================================"
  [ "$FAIL" -eq 0 ] && exit 0 || exit 1

else
  # --- Server-side fast mode ---
  echo "============================================"
  echo "  FluentPDF Smoke Test (server-side)"
  echo "============================================"
  echo ""

  RESULT=$(curl -s --max-time 120 -X POST "$BASE/api/gui/smoke-test" \
    -H "Content-Type: application/json" \
    -d "{\"filePath\":\"$PDF\"}")

  # Parse and display results
  echo "$RESULT" | python -c "
import sys, json
d = json.load(sys.stdin)
for s in d['steps']:
    status = 'PASS' if s['pass'] else 'FAIL'
    err = f\"  {s.get('error','')}\" if not s['pass'] else ''
    print(f\"  {s['step']:<40s} {status}{err}\")
print()
print('============================================')
print(f\"  Results: {d['passed']}/{d['total']} passed\")
print('============================================')
sys.exit(0 if d['allPassed'] else 1)
" 2>/dev/null

fi
