# FluentPDF Avalonia - Installation Guide

This guide provides detailed installation instructions for all supported platforms.

## Table of Contents

- [Windows Installation](#windows-installation)
- [macOS Installation](#macos-installation)
- [Linux Installation](#linux-installation)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)
- [Uninstallation](#uninstallation)

## Windows Installation

### Prerequisites

- Windows 10 version 1809 or later, or Windows 11
- x64 or ARM64 processor
- ~80 MB disk space

### Standard Installation

1. **Download**
   ```
   FluentPDF-win-x64-v1.0.0.zip
   FluentPDF-win-x64-v1.0.0.zip.sha256
   ```

2. **Verify Checksum** (Optional but Recommended)
   ```powershell
   # Calculate hash
   $hash = Get-FileHash -Path "FluentPDF-win-x64-v1.0.0.zip" -Algorithm SHA256

   # Display hash
   Write-Host $hash.Hash

   # Compare with provided checksum
   Get-Content "FluentPDF-win-x64-v1.0.0.zip.sha256"
   ```

3. **Extract**
   - Right-click `FluentPDF-win-x64-v1.0.0.zip`
   - Select "Extract All..."
   - Choose destination (e.g., `C:\Program Files\FluentPDF`)
   - Click "Extract"

4. **Create Shortcut** (Optional)
   - Right-click `FluentPDF.Avalonia.exe`
   - Select "Create shortcut"
   - Move shortcut to Desktop or Start Menu

5. **Launch**
   - Double-click `FluentPDF.Avalonia.exe`
   - If Windows SmartScreen appears:
     - Click "More info"
     - Click "Run anyway"

### PowerShell Installation Script

```powershell
# Download and install FluentPDF
$version = "1.0.0"
$installDir = "$env:ProgramFiles\FluentPDF"
$zipFile = "FluentPDF-win-x64-v$version.zip"
$downloadUrl = "https://github.com/yourusername/FluentPDF/releases/download/v$version/$zipFile"

# Create install directory
New-Item -ItemType Directory -Path $installDir -Force

# Download
Invoke-WebRequest -Uri $downloadUrl -OutFile $zipFile

# Verify checksum (if available)
$checksumUrl = "$downloadUrl.sha256"
$expectedHash = (Invoke-WebRequest -Uri $checksumUrl).Content.Split()[0]
$actualHash = (Get-FileHash -Path $zipFile -Algorithm SHA256).Hash

if ($actualHash -ne $expectedHash) {
    Write-Error "Checksum verification failed!"
    exit 1
}

# Extract
Expand-Archive -Path $zipFile -DestinationPath $installDir -Force

# Create start menu shortcut
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\FluentPDF.lnk")
$Shortcut.TargetPath = "$installDir\FluentPDF.Avalonia.exe"
$Shortcut.Save()

Write-Host "FluentPDF installed successfully to $installDir"
```

### File Association (Optional)

To set FluentPDF as the default PDF viewer:

1. Right-click any PDF file
2. Select "Open with" → "Choose another app"
3. Click "More apps" → "Look for another app on this PC"
4. Navigate to installation directory
5. Select `FluentPDF.Avalonia.exe`
6. Check "Always use this app to open .pdf files"
7. Click "OK"

## macOS Installation

### Prerequisites

- macOS 10.15 (Catalina) or later
- Intel or Apple Silicon processor
- ~80 MB disk space

### Standard Installation

1. **Download**
   - Intel Macs: `FluentPDF-osx-x64-v1.0.0.zip`
   - Apple Silicon: `FluentPDF-osx-arm64-v1.0.0.zip`

2. **Verify Checksum** (Optional)
   ```bash
   # Calculate hash
   shasum -a 256 FluentPDF-osx-x64-v1.0.0.zip

   # Compare with provided checksum
   cat FluentPDF-osx-x64-v1.0.0.zip.sha256
   ```

3. **Extract and Install**
   ```bash
   # Extract
   unzip FluentPDF-osx-x64-v1.0.0.zip

   # Move to Applications
   mv FluentPDF.Avalonia.app /Applications/

   # Remove quarantine flag (bypass Gatekeeper)
   xattr -d com.apple.quarantine /Applications/FluentPDF.Avalonia.app
   ```

4. **Launch**
   - Method 1: Double-click in Applications folder
   - Method 2: Right-click → Open (for first launch)
   - Method 3: Terminal: `open /Applications/FluentPDF.Avalonia.app`

### Homebrew Installation (Future)

```bash
# Coming soon
brew install --cask fluentpdf
```

### File Association (Optional)

1. Right-click any PDF file
2. Select "Get Info"
3. Under "Open with:", select "FluentPDF"
4. Click "Change All..."
5. Click "Continue"

## Linux Installation

### Prerequisites

- Ubuntu 20.04+, Fedora 36+, Debian 11+, or equivalent
- x64 or ARM64 processor
- ~80 MB disk space
- X11 or Wayland display server

### System Dependencies

**Ubuntu/Debian:**
```bash
sudo apt-get update
sudo apt-get install -y libx11-6 libice6 libsm6 libfontconfig1
```

**Fedora/RHEL:**
```bash
sudo dnf install -y libX11 libICE libSM fontconfig
```

**Arch Linux:**
```bash
sudo pacman -S libx11 libice libsm fontconfig
```

### Standard Installation

1. **Download**
   ```bash
   wget https://github.com/yourusername/FluentPDF/releases/download/v1.0.0/FluentPDF-linux-x64-v1.0.0.tar.gz
   wget https://github.com/yourusername/FluentPDF/releases/download/v1.0.0/FluentPDF-linux-x64-v1.0.0.tar.gz.sha256
   ```

2. **Verify Checksum** (Optional)
   ```bash
   # Calculate and compare
   sha256sum -c FluentPDF-linux-x64-v1.0.0.tar.gz.sha256
   ```

3. **Extract and Install**
   ```bash
   # Extract
   tar -xzf FluentPDF-linux-x64-v1.0.0.tar.gz

   # Install system-wide
   sudo mv FluentPDF.Avalonia /usr/local/bin/
   sudo chmod +x /usr/local/bin/FluentPDF.Avalonia

   # Or install for current user
   mkdir -p ~/.local/bin
   mv FluentPDF.Avalonia ~/.local/bin/
   chmod +x ~/.local/bin/FluentPDF.Avalonia
   ```

4. **Launch**
   ```bash
   FluentPDF.Avalonia
   ```

### Desktop Entry (Optional)

Create desktop launcher:

```bash
cat > ~/.local/share/applications/fluentpdf.desktop << EOF
[Desktop Entry]
Type=Application
Name=FluentPDF
Comment=Cross-platform PDF viewer
Exec=/usr/local/bin/FluentPDF.Avalonia %f
Icon=application-pdf
Terminal=false
Categories=Office;Viewer;
MimeType=application/pdf;
EOF

# Update desktop database
update-desktop-database ~/.local/share/applications/
```

### File Association

**GNOME:**
```bash
xdg-mime default fluentpdf.desktop application/pdf
```

**KDE Plasma:**
1. Right-click PDF file → Properties
2. Click "Application Preference Order"
3. Add FluentPDF to top of list

## Verification

After installation, verify FluentPDF works correctly:

### Windows
```powershell
# Check version
.\FluentPDF.Avalonia.exe --version

# Open test PDF
.\FluentPDF.Avalonia.exe "C:\path\to\test.pdf"
```

### macOS/Linux
```bash
# Check version
./FluentPDF.Avalonia --version

# Open test PDF
./FluentPDF.Avalonia /path/to/test.pdf
```

### Verification Checklist

- [ ] Application launches without errors
- [ ] Can open file dialog
- [ ] Can load and display PDF files
- [ ] Can switch between pages
- [ ] Theme toggle works (dark/light)
- [ ] No missing library errors
- [ ] No console window appears (Windows)

## Troubleshooting

### Windows Issues

**SmartScreen Warning:**
- Expected for unsigned executables
- Click "More info" → "Run anyway"
- Consider code signing for production

**Missing DLLs:**
- Ensure using self-contained build
- Try running from Command Prompt to see error details:
  ```powershell
  .\FluentPDF.Avalonia.exe
  ```

**Application Won't Start:**
```powershell
# Check Windows Event Viewer
eventvwr.msc
# Navigate to: Windows Logs → Application
# Look for FluentPDF errors
```

### macOS Issues

**"FluentPDF.Avalonia.app is damaged":**
```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine /Applications/FluentPDF.Avalonia.app

# If that fails, remove all attributes
xattr -cr /Applications/FluentPDF.Avalonia.app
```

**Gatekeeper Blocks Launch:**
```bash
# Allow app through Gatekeeper
spctl --add /Applications/FluentPDF.Avalonia.app
```

**Library Load Errors:**
```bash
# Check dependencies
otool -L /Applications/FluentPDF.Avalonia.app/Contents/MacOS/FluentPDF.Avalonia
```

### Linux Issues

**Missing Libraries:**
```bash
# Check which libraries are missing
ldd ./FluentPDF.Avalonia | grep "not found"

# Install missing packages (Ubuntu/Debian)
sudo apt-get install -y libx11-6 libice6 libsm6 libfontconfig1
```

**Display Server Issues:**
```bash
# Check display
echo $DISPLAY

# If empty, set it
export DISPLAY=:0
```

**Permission Denied:**
```bash
# Make executable
chmod +x FluentPDF.Avalonia
```

**Wayland Compatibility:**
```bash
# Force X11 backend if Wayland issues
GDK_BACKEND=x11 ./FluentPDF.Avalonia
```

### Log Files

FluentPDF logs diagnostic information to:

- **Windows:** `%APPDATA%\FluentPDF\logs\`
- **macOS:** `~/Library/Logs/FluentPDF/`
- **Linux:** `~/.local/share/FluentPDF/logs/`

Check logs for error details:
```bash
# View latest log
tail -f ~/.local/share/FluentPDF/logs/fluentpdf-latest.log
```

## Uninstallation

### Windows

**Manual:**
1. Delete installation folder (e.g., `C:\Program Files\FluentPDF`)
2. Delete shortcuts from Desktop/Start Menu
3. Delete app data: `%APPDATA%\FluentPDF`

**PowerShell:**
```powershell
$installDir = "$env:ProgramFiles\FluentPDF"
Remove-Item -Path $installDir -Recurse -Force
Remove-Item -Path "$env:APPDATA\FluentPDF" -Recurse -Force
```

### macOS

```bash
# Remove application
rm -rf /Applications/FluentPDF.Avalonia.app

# Remove user data
rm -rf ~/Library/Application\ Support/FluentPDF
rm -rf ~/Library/Logs/FluentPDF
```

### Linux

```bash
# Remove executable
sudo rm /usr/local/bin/FluentPDF.Avalonia
# Or for user install
rm ~/.local/bin/FluentPDF.Avalonia

# Remove desktop entry
rm ~/.local/share/applications/fluentpdf.desktop

# Remove user data
rm -rf ~/.local/share/FluentPDF
```

## Support

For additional help:

- Documentation: `docs/USER_GUIDE.md`
- Issues: GitHub Issues
- Discussions: GitHub Discussions

## Next Steps

After installation:

1. Read the [User Guide](USER_GUIDE.md)
2. Explore keyboard shortcuts
3. Customize theme preferences
4. Report any issues on GitHub

---

**Need help?** Open an issue on GitHub with:
- Operating system and version
- Installation method used
- Error messages or logs
- Steps to reproduce the problem
