# FluentPDF Scripts

## UAT.ps1 - User Acceptance Testing Script

Comprehensive UAT script that builds the app, launches it with logging, and monitors logs in real-time.

### Usage

```powershell
# Basic UAT (builds and launches with bookmarked.pdf)
.\scripts\UAT.ps1

# UAT with different PDF file
.\scripts\UAT.ps1 -TestPdf "path\to\your.pdf"

# Skip build (use existing binaries)
.\scripts\UAT.ps1 -SkipBuild

# Clean build (removes old binaries first)
.\scripts\UAT.ps1 -CleanBuild

# Verbose build output
.\scripts\UAT.ps1 -Verbose

# Combine options
.\scripts\UAT.ps1 -TestPdf "tests\Fixtures\complex-layout.pdf" -CleanBuild -Verbose
```

### What It Does

1. **Builds the app** (unless `-SkipBuild`)
   - Optional: Clean build with `-CleanBuild`
   - Logs build output to timestamped file

2. **Launches FluentPDF**
   - Opens specified test PDF
   - Clears old application logs

3. **Monitors logs in real-time**
   - Tails application log file
   - Color-codes errors (red), warnings (yellow), info (gray)
   - Shows logs in console as they happen

4. **Saves session logs**
   - Location: `logs/UAT_yyyy-MM-dd_HH-mm-ss.log`
   - Contains: Build output, launch info, and all app logs

### Workflow

1. **User runs script**:
   ```powershell
   .\scripts\UAT.ps1
   ```

2. **User performs testing**:
   - Open PDFs
   - Test features
   - Try to reproduce bugs
   - Observe behavior

3. **User provides feedback**:
   - Example: "Thumbnails not showing"
   - Example: "Crashed when I typed Japanese"

4. **Developer reads logs**:
   ```powershell
   # I read the session log
   Get-Content logs\UAT_2026-01-17_18-30-00.log

   # Or the raw app log
   Get-Content $env:LOCALAPPDATA\Temp\FluentPDF\logs\log-20260117.json
   ```

5. **Developer implements fixes** based on log analysis

### Test Checklist

The script displays this checklist during UAT:

- [ ] App launches without crash
- [ ] PDF displays correctly
- [ ] Thumbnails appear on left sidebar
- [ ] Bookmarks panel behavior (hide when empty)
- [ ] Page navigation (forward/backward)
- [ ] Zoom controls (image scales, area stays constant)
- [ ] Japanese IME input (no crash)
- [ ] Search functionality

### Log Locations

- **Session logs**: `logs/UAT_<timestamp>.log` (build + app logs combined)
- **App logs**: `%LOCALAPPDATA%\Temp\FluentPDF\logs\log-<date>.json`

### Examples

#### Example 1: Basic UAT
```powershell
.\scripts\UAT.ps1
```
Output:
```
[18:30:00] ========================================
[18:30:00] FluentPDF UAT Session Started
[18:30:00] ========================================
[18:30:00] Log file: logs\UAT_2026-01-17_18-30-00.log
[18:30:00]
[18:30:00] [1/4] Skipping clean
[18:30:00] [2/4] Building FluentPDF.App...
[18:30:15] ✓ Build successful
[18:30:15] [3/4] Verifying application...
[18:30:15] ✓ App found
[18:30:15] [4/4] Checking for running instances...
[18:30:15]   No running instances found
[18:30:15]
[18:30:15] ========================================
[18:30:15] Launching FluentPDF for UAT Testing
[18:30:15] ========================================
```

#### Example 2: Clean Build with Custom PDF
```powershell
.\scripts\UAT.ps1 -CleanBuild -TestPdf "tests\Fixtures\complex-layout.pdf"
```

#### Example 3: Quick Test (Skip Build)
```powershell
.\scripts\UAT.ps1 -SkipBuild
```

### Tips

- **First run**: Use `-CleanBuild` to ensure fresh binaries
- **Quick iterations**: Use `-SkipBuild` when only testing
- **Debugging**: Use `-Verbose` to see detailed build output
- **Log analysis**: Session logs are saved automatically - provide feedback and let me read them

### Troubleshooting

**Q: Build fails with errors**
```powershell
# Try clean build
.\scripts\UAT.ps1 -CleanBuild -Verbose
```

**Q: App doesn't launch**
```powershell
# Check if app exists
Test-Path "src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"
```

**Q: Logs not appearing**
```powershell
# Check log directory
Get-ChildItem $env:LOCALAPPDATA\Temp\FluentPDF\logs\
```

**Q: Want to see more logs**
```powershell
# The script automatically tails logs in real-time
# All logs are saved to the session file
```
