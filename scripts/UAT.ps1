# FluentPDF User Acceptance Testing (UAT) Script
# This script builds the app, launches it with logging, and monitors logs in real-time

param(
    [string]$TestPdf = "tests\Fixtures\bookmarked.pdf",
    [switch]$SkipBuild = $false,
    [switch]$CleanBuild = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

# Configuration
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$AppProject = "$ProjectRoot\src\FluentPDF.App\FluentPDF.App.csproj"
$AppExePath = "$ProjectRoot\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"
$LogsDir = "$ProjectRoot\logs"
$AppLogDir = "$env:LOCALAPPDATA\Temp\FluentPDF\logs"

# Create logs directory
if (-not (Test-Path $LogsDir)) {
    New-Item -ItemType Directory -Path $LogsDir -Force | Out-Null
}

# Generate timestamped log filename
$Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$UatLogFile = "$LogsDir\UAT_$Timestamp.log"

# Helper function to write to both console and log
function Write-Log {
    param([string]$Message, [string]$Color = "White")

    $TimestampedMessage = "[$(Get-Date -Format 'HH:mm:ss')] $Message"
    Write-Host $TimestampedMessage -ForegroundColor $Color
    Add-Content -Path $UatLogFile -Value $TimestampedMessage
}

# Start UAT session
Write-Log "========================================" "Cyan"
Write-Log "FluentPDF UAT Session Started" "Cyan"
Write-Log "========================================" "Cyan"
Write-Log "Log file: $UatLogFile" "Gray"
Write-Log ""

# Step 1: Clean old binaries (if requested)
if ($CleanBuild) {
    Write-Log "[1/4] Cleaning old binaries..." "Yellow"
    try {
        dotnet clean $AppProject -p:Platform=x64 --verbosity minimal 2>&1 | Tee-Object -FilePath $UatLogFile -Append
        Write-Log "✓ Clean completed" "Green"
    }
    catch {
        Write-Log "✗ Clean failed: $_" "Red"
        exit 1
    }
}
else {
    Write-Log "[1/4] Skipping clean (use -CleanBuild to clean)" "Gray"
}

Write-Log ""

# Step 2: Build the application
if (-not $SkipBuild) {
    Write-Log "[2/4] Building FluentPDF.App..." "Yellow"
    try {
        $BuildVerbosity = if ($Verbose) { "normal" } else { "minimal" }
        dotnet build $AppProject -p:Platform=x64 --verbosity $BuildVerbosity 2>&1 | Tee-Object -FilePath $UatLogFile -Append

        if ($LASTEXITCODE -ne 0) {
            Write-Log "✗ Build failed with exit code $LASTEXITCODE" "Red"
            exit $LASTEXITCODE
        }

        Write-Log "✓ Build successful" "Green"
    }
    catch {
        Write-Log "✗ Build failed: $_" "Red"
        exit 1
    }
}
else {
    Write-Log "[2/4] Skipping build (use without -SkipBuild to build)" "Gray"
}

Write-Log ""

# Step 3: Verify app exists
Write-Log "[3/4] Verifying application..." "Yellow"
if (-not (Test-Path $AppExePath)) {
    Write-Log "✗ App not found at: $AppExePath" "Red"
    Write-Log "  Please build the app first or remove -SkipBuild flag" "Red"
    exit 1
}
Write-Log "✓ App found: $AppExePath" "Green"
Write-Log ""

# Step 4: Kill any existing instances
Write-Log "[4/4] Checking for running instances..." "Yellow"
$RunningProcesses = Get-Process FluentPDF.App -ErrorAction SilentlyContinue
if ($RunningProcesses) {
    Write-Log "  Found $($RunningProcesses.Count) running instance(s)" "Yellow"
    $RunningProcesses | Stop-Process -Force
    Start-Sleep -Seconds 1
    Write-Log "  ✓ Stopped all running instances" "Green"
}
else {
    Write-Log "  No running instances found" "Gray"
}
Write-Log ""

# Clear application logs before launch
if (Test-Path $AppLogDir) {
    Write-Log "Clearing old application logs..." "Yellow"
    Get-ChildItem $AppLogDir -Filter "log-*.json" | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Log "✓ Application logs cleared" "Green"
}
Write-Log ""

# Launch the application
Write-Log "========================================" "Cyan"
Write-Log "Launching FluentPDF for UAT Testing" "Cyan"
Write-Log "========================================" "Cyan"
Write-Log ""
Write-Log "Test PDF: $TestPdf" "White"
Write-Log "App log directory: $AppLogDir" "Gray"
Write-Log ""

# Resolve test PDF path
$TestPdfPath = Join-Path $ProjectRoot $TestPdf
if (-not (Test-Path $TestPdfPath)) {
    Write-Log "⚠ Warning: Test PDF not found: $TestPdfPath" "Yellow"
    Write-Log "  App will launch without opening a file" "Yellow"
    $TestPdfPath = $null
}

# Launch the app
Write-Log "Starting FluentPDF.App..." "Green"
if ($TestPdfPath) {
    $Process = Start-Process -FilePath $AppExePath -ArgumentList "`"$TestPdfPath`"" -PassThru
}
else {
    $Process = Start-Process -FilePath $AppExePath -PassThru
}

Write-Log "✓ App launched (PID: $($Process.Id))" "Green"
Write-Log ""

# Wait for app to initialize and logs to be created
Write-Log "Waiting for application to initialize..." "Yellow"
Start-Sleep -Seconds 3

# Find the current log file
$CurrentLogFile = $null
$MaxAttempts = 10
$Attempt = 0

while ($Attempt -lt $MaxAttempts -and $null -eq $CurrentLogFile) {
    $Attempt++
    if (Test-Path $AppLogDir) {
        $LogFiles = Get-ChildItem $AppLogDir -Filter "log-*.json" -ErrorAction SilentlyContinue |
                    Sort-Object LastWriteTime -Descending |
                    Select-Object -First 1

        if ($LogFiles) {
            $CurrentLogFile = $LogFiles.FullName
            break
        }
    }

    if ($null -eq $CurrentLogFile) {
        Write-Log "  Waiting for log file... (attempt $Attempt/$MaxAttempts)" "Gray"
        Start-Sleep -Seconds 1
    }
}

if ($null -eq $CurrentLogFile) {
    Write-Log "⚠ Warning: Could not find application log file" "Yellow"
    Write-Log "  App is running but logs may not be available" "Yellow"
    Write-Log ""
    Write-Log "========================================" "Cyan"
    Write-Log "UAT SESSION ACTIVE" "Cyan"
    Write-Log "========================================" "Cyan"
    Write-Log ""
    Write-Log "Please test the application and provide feedback." "White"
    Write-Log "Press Ctrl+C to exit and stop monitoring." "Gray"
    Write-Log ""

    # Just wait for user to close the app
    try {
        $Process.WaitForExit()
    }
    catch {
        # User pressed Ctrl+C
    }

    exit 0
}

Write-Log "✓ Found application log: $CurrentLogFile" "Green"
Write-Log ""

# Display UAT instructions
Write-Log "========================================" "Cyan"
Write-Log "UAT SESSION ACTIVE - MONITORING LOGS" "Cyan"
Write-Log "========================================" "Cyan"
Write-Log ""
Write-Log "INSTRUCTIONS:" "White"
Write-Log "1. Perform your testing in the FluentPDF window" "White"
Write-Log "2. Logs will appear below in real-time" "White"
Write-Log "3. When done, close the app or press Ctrl+C here" "White"
Write-Log "4. Provide feedback - I'll read the log file" "White"
Write-Log ""
Write-Log "TEST CHECKLIST:" "Yellow"
Write-Log "  [ ] App launches without crash" "Gray"
Write-Log "  [ ] PDF displays correctly" "Gray"
Write-Log "  [ ] Thumbnails appear on left sidebar" "Gray"
Write-Log "  [ ] Bookmarks panel behavior (hide when empty)" "Gray"
Write-Log "  [ ] Page navigation (forward/backward)" "Gray"
Write-Log "  [ ] Zoom controls (image scales, area stays constant)" "Gray"
Write-Log "  [ ] Japanese IME input (no crash)" "Gray"
Write-Log "  [ ] Search functionality" "Gray"
Write-Log ""
Write-Log "========================================" "Cyan"
Write-Log "LIVE APPLICATION LOGS:" "Cyan"
Write-Log "========================================" "Cyan"
Write-Log ""

# Monitor the log file in real-time
try {
    # Get initial log content
    $LastSize = (Get-Item $CurrentLogFile).Length

    # Tail the log file
    Get-Content $CurrentLogFile -Tail 20 | ForEach-Object {
        Write-Log $_ "DarkGray"
    }

    Write-Log ""
    Write-Log "--- Monitoring new log entries (press Ctrl+C to stop) ---" "DarkYellow"
    Write-Log ""

    while ($true) {
        # Check if app is still running
        if ($Process.HasExited) {
            Write-Log ""
            Write-Log "✓ Application exited normally" "Green"
            break
        }

        # Check for new content
        if (Test-Path $CurrentLogFile) {
            $CurrentSize = (Get-Item $CurrentLogFile).Length

            if ($CurrentSize -gt $LastSize) {
                # Read new content
                $Stream = [System.IO.File]::Open($CurrentLogFile, 'Open', 'Read', 'ReadWrite')
                $Stream.Seek($LastSize, [System.IO.SeekOrigin]::Begin) | Out-Null

                $Reader = New-Object System.IO.StreamReader($Stream)
                while (-not $Reader.EndOfStream) {
                    $Line = $Reader.ReadLine()

                    # Color code based on log level
                    $Color = "DarkGray"
                    if ($Line -match '"@l":"Error"') { $Color = "Red" }
                    elseif ($Line -match '"@l":"Warning"') { $Color = "Yellow" }
                    elseif ($Line -match '"@l":"Fatal"') { $Color = "Magenta" }
                    elseif ($Line -match 'Exception|Error|crash') { $Color = "Red" }

                    Write-Log $Line $Color
                }

                $Reader.Close()
                $Stream.Close()

                $LastSize = $CurrentSize
            }
        }

        Start-Sleep -Milliseconds 500
    }
}
catch {
    Write-Log ""
    Write-Log "Monitoring stopped: $_" "Yellow"
}
finally {
    Write-Log ""
    Write-Log "========================================" "Cyan"
    Write-Log "UAT SESSION ENDED" "Cyan"
    Write-Log "========================================" "Cyan"
    Write-Log ""
    Write-Log "Summary:" "White"
    Write-Log "  Session log: $UatLogFile" "Gray"
    Write-Log "  Application log: $CurrentLogFile" "Gray"
    Write-Log ""
    Write-Log "Next steps:" "Yellow"
    Write-Log "  1. Provide your feedback" "White"
    Write-Log "  2. I'll read the logs to diagnose issues" "White"
    Write-Log "  3. I'll implement fixes based on your feedback" "White"
    Write-Log ""

    # Check if app is still running
    if (-not $Process.HasExited) {
        Write-Log "App is still running (PID: $($Process.Id))" "Yellow"
        Write-Log "Press any key to close the app..." "Gray"
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        $Process | Stop-Process -Force
        Write-Log "✓ App closed" "Green"
    }
}
