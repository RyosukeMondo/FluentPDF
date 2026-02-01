#!/bin/bash
# FluentPDF Avalonia - Cross-platform Build Script
# Supports Windows, macOS, and Linux

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Detect platform
PLATFORM="$(uname -s)"
case "$PLATFORM" in
    Linux*)     PLATFORM=linux;;
    Darwin*)    PLATFORM=osx;;
    MINGW*|MSYS*|CYGWIN*) PLATFORM=windows;;
    *)          PLATFORM="unknown";;
esac

echo -e "${GREEN}FluentPDF Avalonia Build Script${NC}"
echo -e "Platform: ${YELLOW}$PLATFORM${NC}"
echo ""

# Configuration
PROJECT_PATH="src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj"
OUTPUT_DIR="artifacts/$PLATFORM"
CONFIGURATION="${1:-Release}"

echo -e "${YELLOW}Building configuration: $CONFIGURATION${NC}"

# Clean previous builds
if [ -d "$OUTPUT_DIR" ]; then
    echo "Cleaning previous build artifacts..."
    rm -rf "$OUTPUT_DIR"
fi

# Restore dependencies
echo -e "\n${YELLOW}Restoring dependencies...${NC}"
dotnet restore "$PROJECT_PATH"

# Build for platform-specific runtime
case "$PLATFORM" in
    linux)
        RUNTIME="linux-x64"
        ;;
    osx)
        RUNTIME="osx-x64"
        ;;
    windows)
        RUNTIME="win-x64"
        ;;
    *)
        echo -e "${RED}Unsupported platform: $PLATFORM${NC}"
        exit 1
        ;;
esac

echo -e "\n${YELLOW}Building for runtime: $RUNTIME${NC}"
dotnet build "$PROJECT_PATH" \
    --configuration "$CONFIGURATION" \
    --runtime "$RUNTIME" \
    --output "$OUTPUT_DIR"

# Run tests
echo -e "\n${YELLOW}Running tests...${NC}"
dotnet test tests/FluentPDF.Core.Tests/FluentPDF.Core.Tests.csproj --configuration "$CONFIGURATION"
dotnet test tests/FluentPDF.Rendering.Tests/FluentPDF.Rendering.Tests.csproj --configuration "$CONFIGURATION"

echo -e "\n${GREEN}Build completed successfully!${NC}"
echo -e "Output directory: ${YELLOW}$OUTPUT_DIR${NC}"

# Display build info
if [ -f "$OUTPUT_DIR/FluentPDF.Avalonia" ] || [ -f "$OUTPUT_DIR/FluentPDF.Avalonia.exe" ]; then
    echo -e "\n${GREEN}Executable:${NC}"
    ls -lh "$OUTPUT_DIR"/FluentPDF.Avalonia* 2>/dev/null | grep -v '.dll\|.pdb'
else
    echo -e "\n${RED}Warning: Executable not found${NC}"
fi

echo -e "\n${YELLOW}To run the application:${NC}"
case "$PLATFORM" in
    linux|osx)
        echo "  cd $OUTPUT_DIR && ./FluentPDF.Avalonia"
        ;;
    windows)
        echo "  cd $OUTPUT_DIR && FluentPDF.Avalonia.exe"
        ;;
esac
