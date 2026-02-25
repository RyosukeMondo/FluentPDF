#!/bin/sh
#
# File Size KPI Pre-Commit Hook
# Enforces max 500 lines per file (excluding comments/blanks)
#
# To install this hook:
# 1. Copy to .git/hooks/pre-commit
# 2. Make executable: chmod +x .git/hooks/pre-commit
#
# Or run: tools/install-hooks.sh
#

echo "🔍 Checking file size KPI compliance..."

# Run PowerShell script
if command -v pwsh &> /dev/null; then
    pwsh -File tools/check-file-sizes.ps1
    EXIT_CODE=$?
elif command -v powershell &> /dev/null; then
    powershell -File tools/check-file-sizes.ps1
    EXIT_CODE=$?
else
    echo "⚠️  WARNING: PowerShell not found. Skipping file size check."
    echo "Install PowerShell Core to enable this check: https://github.com/PowerShell/PowerShell"
    exit 0
fi

if [ $EXIT_CODE -ne 0 ]; then
    echo ""
    echo "❌ Commit rejected: File size KPI violation"
    echo ""
    echo "One or more files exceed the 500-line limit."
    echo "See DEVELOPER_QUICK_REFERENCE.md for refactoring strategies."
    echo ""
    echo "Quick fixes:"
    echo "  1. Extract configuration/preset logic → ConfigurationService"
    echo "  2. Consolidate duplicate methods → Handler"
    echo "  3. Move platform-specific code → PlatformDetector"
    echo "  4. Extract validation/helper logic → Helper class"
    echo ""
    echo "Or use --no-verify to bypass (not recommended):"
    echo "  git commit --no-verify -m \"your message\""
    exit 1
fi

echo "✅ File size KPI check passed!"
exit 0
