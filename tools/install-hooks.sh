#!/bin/bash
#
# Install Git Hooks for FluentPDF
# Installs pre-commit hook for file size enforcement
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
HOOKS_DIR="$REPO_ROOT/.git/hooks"

echo "Installing Git hooks for FluentPDF..."

# Check if .git directory exists
if [ ! -d "$REPO_ROOT/.git" ]; then
    echo "❌ Error: Not a git repository. Run from FluentPDF root."
    exit 1
fi

# Create hooks directory if it doesn't exist
mkdir -p "$HOOKS_DIR"

# Install pre-commit hook
echo "📋 Installing pre-commit hook..."
cp "$SCRIPT_DIR/pre-commit-hook-template.sh" "$HOOKS_DIR/pre-commit"
chmod +x "$HOOKS_DIR/pre-commit"

echo "✅ Hooks installed successfully!"
echo ""
echo "Installed hooks:"
echo "  - pre-commit: Enforces 500-line KPI"
echo ""
echo "To test the hook, try:"
echo "  git commit -m 'test'"
echo ""
echo "To bypass the hook (not recommended):"
echo "  git commit --no-verify -m 'your message'"
